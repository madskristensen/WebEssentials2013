using System.IO;
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
        private static string Require(string modulePath)
        {
            return NodeModuleService.ResolveModule(SourceDirectory, modulePath);
        }

        private static void AssertRequire(string modulePath, string expectedFile, string message = null)
        {
            Assert.AreEqual(
                expectedFile == null ? null : Path.GetFullPath(Path.Combine(TargetFixturesDir, expectedFile)),
                Require(modulePath),
                message ?? ("require('" + modulePath + "') failed")
            );
        }
        #endregion

        [TestMethod]
        public void BasicResolveTest()
        {
            // Custom Tests:
            AssertRequire("../module-resolution/packages/main/package", "packages/main/package.json");

            // Imported from Node.js:
            AssertRequire("../module-resolution/a.js", "a.js");

            // require a file without any extensions
            AssertRequire("../module-resolution/foo", "foo");
            AssertRequire("../module-resolution/a", "a.js");
            AssertRequire("../module-resolution/b/c", "b/c.js");
            AssertRequire("../module-resolution/b/d", "b/d.js");

            // Test loading relative paths from different files (adapted from require()s spread across files)
            Assert.AreEqual(Require("./module-resolution/b/c.js"), NodeModuleService.ResolveModule("./b/c", Require("../module-resolution/a")));
            Assert.AreEqual(Require("./module-resolution/b/d.js"), NodeModuleService.ResolveModule("./d", Require("../module-resolution/b/c")));
            Assert.AreEqual(Require("./module-resolution/b/package/index.js"), NodeModuleService.ResolveModule("./package", Require("../module-resolution/b/c")));

            // Absolute
            // I see no reason to support absolute paths.
            //Assert.AreEqual(Require("../module-resolution/b/d"), Require(Path.Combine(SourceDirectory, "../module-resolution/b/d")));

            // Adapted from test index.js modules ids and relative loading
            Assert.AreNotEqual(NodeModuleService.ResolveModule(Path.GetDirectoryName(Require("../module-resolution/nested-index/two")), "./hello"),
                               NodeModuleService.ResolveModule(Path.GetDirectoryName(Require("../module-resolution/nested-index/one")), "./hello")
                              );

            AssertRequire("../module-resolution/empty", null);
        }

        [TestMethod]
        public void TrailingSlashTest()
        {
            // Adapted from test index.js in a folder with a trailing slash
            string three = Require("../module-resolution/nested-index/three"),
                   threeFolder = Require("../module-resolution/nested-index/three/"),
                   threeIndex = Require("../module-resolution/nested-index/three/index.js");
            Assert.AreEqual(threeFolder, threeIndex);
            Assert.AreNotEqual(threeFolder, three);
        }

        [TestMethod]
        public void PackageJsonTest()
        {
            // Adapted from test package.json require() loading
            AssertRequire("../module-resolution/packages/main", "packages/main/package-main-module.js", "Failed loading package");
            AssertRequire("../module-resolution/packages/main-index", "packages/main-index/package-main-module/index.js", "Failed loading package with index.js in main subdir");
        }

        [TestMethod]
        public void ParentDirCyclesTest()
        {
            // Adapted from test cycles containing a .. path
            Assert.AreEqual(Require("./module-resolution/cycles/folder/foo.js"), NodeModuleService.ResolveModule("./folder/foo", Require("../module-resolution/cycles/root")));
            Assert.AreEqual(Require("./module-resolution/cycles/root.js"), NodeModuleService.ResolveModule("./../root", Require("../module-resolution/cycles/folder/foo")));
        }

        [TestMethod]
        public void NodeModulesTest()
        {
            var basePath = Path.Combine(TargetFixturesDir, "node_modules");

            // Custom Tests:
            Assert.AreEqual(Require("../module-resolution/node_modules/baz/otherFile.js"), NodeModuleService.ResolveModule(basePath, "baz/otherFile"));

            // Imported from Node.js:

            // Adapted from test node_modules folders

            Assert.AreEqual(Require("../module-resolution/node_modules/baz/index.js"), NodeModuleService.ResolveModule(basePath, "baz"));
            Assert.AreEqual(NodeModuleService.ResolveModule(basePath, "./baz/index.js"), NodeModuleService.ResolveModule(basePath, "baz"));

            basePath += @"\baz";
            Assert.AreEqual(NodeModuleService.ResolveModule(basePath, "../bar.js"), NodeModuleService.ResolveModule(basePath, "bar"));
            Assert.AreEqual(Require("../module-resolution/node_modules/bar.js"), NodeModuleService.ResolveModule(basePath, "bar"));

            Assert.AreEqual(NodeModuleService.ResolveModule(basePath, "./node_modules/asdf.js"), NodeModuleService.ResolveModule(basePath, "asdf"));
            Assert.AreEqual(Require("../module-resolution/node_modules/baz/node_modules/asdf"), NodeModuleService.ResolveModule(basePath, "asdf"));
        }
    }
}
