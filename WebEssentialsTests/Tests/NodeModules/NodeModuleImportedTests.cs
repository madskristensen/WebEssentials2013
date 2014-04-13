using System.IO;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    ///<summary>This is a port of https://github.com/joyent/node/blob/master/test/simple/test-module-loading.js, to test our module resolution logic against the original.</summary>
    ///<remarks>Most of these tests test loading the modules rather than resolving them, and have been omitted.</remarks>
    [TestClass]
    public class NodeModuleImportedTests
    {
        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);
        private static readonly string TargetFixturesDir = Path.Combine(BaseDirectory, @"fixtures\module-resolution");
        private static readonly string SourceDirectory = Path.Combine(BaseDirectory, @"fixtures\fake-node-source");

        #region Helper Methods
        private async static Task<string> Require(string modulePath)
        {
            return await NodeModuleService.ResolveModule(SourceDirectory, modulePath);
        }

        private async static Task AssertRequire(string modulePath, string expectedFile, string message = null)
        {
            Assert.AreEqual(
                expectedFile == null ? null : Path.GetFullPath(Path.Combine(TargetFixturesDir, expectedFile)),
                await Require(modulePath),
                message ?? ("require('" + modulePath + "') failed")
            );
        }
        #endregion

        [TestMethod]
        public async Task BasicResolveTest()
        {
            // Custom Tests:
            await AssertRequire("../module-resolution/packages/main/package", "packages/main/package.json");

            // Imported from Node.js:
            await AssertRequire("../module-resolution/a.js", "a.js");

            // require a file without any extensions
            await AssertRequire("../module-resolution/foo", "foo");
            await AssertRequire("../module-resolution/a", "a.js");
            await AssertRequire("../module-resolution/b/c", "b/c.js");
            await AssertRequire("../module-resolution/b/d", "b/d.js");

            // Test loading relative paths from different files (adapted from require()s spread across files)
            Assert.AreEqual(await Require("./module-resolution/b/c.js"), await NodeModuleService.ResolveModule("./b/c", await Require("../module-resolution/a")));
            Assert.AreEqual(await Require("./module-resolution/b/d.js"), await NodeModuleService.ResolveModule("./d", await Require("../module-resolution/b/c")));
            Assert.AreEqual(await Require("./module-resolution/b/package/index.js"), await NodeModuleService.ResolveModule("./package", await Require("../module-resolution/b/c")));

            // Absolute
            // I see no reason to support absolute paths.
            //Assert.AreEqual(Require("../module-resolution/b/d"), Require(Path.Combine(SourceDirectory, "../module-resolution/b/d")));

            // Adapted from test index.js modules ids and relative loading
            Assert.AreNotEqual(NodeModuleService.ResolveModule(Path.GetDirectoryName(await Require("../module-resolution/nested-index/two")), "./hello"),
                               NodeModuleService.ResolveModule(Path.GetDirectoryName(await Require("../module-resolution/nested-index/one")), "./hello")
                              );

            await AssertRequire("../module-resolution/empty", null);
        }

        [TestMethod]
        public async Task TrailingSlashTest()
        {
            // Adapted from test index.js in a folder with a trailing slash
            string three = await Require("../module-resolution/nested-index/three"),
                   threeFolder = await Require("../module-resolution/nested-index/three/"),
                   threeIndex = await Require("../module-resolution/nested-index/three/index.js");
            Assert.AreEqual(threeFolder, threeIndex);
            Assert.AreNotEqual(threeFolder, three);
        }

        [TestMethod]
        public async Task PackageJsonTest()
        {
            // Adapted from test package.json require() loading
            await AssertRequire("../module-resolution/packages/main", "packages/main/package-main-module.js", "Failed loading package");
            await AssertRequire("../module-resolution/packages/main-index", "packages/main-index/package-main-module/index.js", "Failed loading package with index.js in main subdir");
        }

        [TestMethod]
        public async Task ParentDirCyclesTest()
        {
            // Adapted from test cycles containing a .. path
            Assert.AreEqual(await Require("./module-resolution/cycles/folder/foo.js"), await NodeModuleService.ResolveModule("./folder/foo", await Require("../module-resolution/cycles/root")));
            Assert.AreEqual(await Require("./module-resolution/cycles/root.js"), await NodeModuleService.ResolveModule("./../root", await Require("../module-resolution/cycles/folder/foo")));
        }

        [TestMethod]
        public async Task NodeModulesTest()
        {
            var basePath = Path.Combine(TargetFixturesDir, "node_modules");

            // Custom Tests:
            Assert.AreEqual(await Require("../module-resolution/node_modules/baz/otherFile.js"), await NodeModuleService.ResolveModule(basePath, "baz/otherFile"));

            // Imported from Node.js:

            // Adapted from test node_modules folders

            Assert.AreEqual(await Require("../module-resolution/node_modules/baz/index.js"), await NodeModuleService.ResolveModule(basePath, "baz"));
            Assert.AreEqual(await NodeModuleService.ResolveModule(basePath, "./baz/index.js"), await NodeModuleService.ResolveModule(basePath, "baz"));

            basePath += @"\baz";
            Assert.AreEqual(await NodeModuleService.ResolveModule(basePath, "../bar.js"), await NodeModuleService.ResolveModule(basePath, "bar"));
            Assert.AreEqual(await Require("../module-resolution/node_modules/bar.js"), await NodeModuleService.ResolveModule(basePath, "bar"));

            Assert.AreEqual(await NodeModuleService.ResolveModule(basePath, "./node_modules/asdf.js"), await NodeModuleService.ResolveModule(basePath, "asdf"));
            Assert.AreEqual(await Require("../module-resolution/node_modules/baz/node_modules/asdf"), await NodeModuleService.ResolveModule(basePath, "asdf"));
        }
    }
}
