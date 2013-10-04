using System;
using MadsKristensen.EditorExtensions;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    ///<summary>This is a port of https://github.com/joyent/node/blob/master/test/simple/test-module-loading.js, to test our module resolution logic against the original.</summary>
    ///<remarks>Most of these tests test loading the modules rather than resolving them, and have been omitted.</remarks>
    [TestClass]
    public class NodeModuleImportedTests
    {
        static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);
        static readonly string TargetFixturesDir = Path.Combine(BaseDirectory, "fixtures");
        static readonly string SourceDirectory = Path.Combine(BaseDirectory, "fake-node-source");

        static string Require(string modulePath)
        {
            return NodeModuleService.ResolveModule(SourceDirectory, modulePath);
        }

        static void AssertRequire(string modulePath, string expectedFile, string message = null)
        {
            Assert.AreEqual(
                expectedFile == null ? null : Path.GetFullPath(Path.Combine(TargetFixturesDir, expectedFile)),
                Require(modulePath),
                message ?? ("require('" + modulePath + "') failed")
            );
        }

        [TestMethod]
        public void BasicResolveTest()
        {
            // Custom Tests:
            AssertRequire("../fixtures/packages/main/package", "packages/main/package.json");

            // Imported from Node.js:
            AssertRequire("../fixtures/a.js", "a.js");

            // require a file without any extensions
            AssertRequire("../fixtures/foo", "foo");
            AssertRequire("../fixtures/a", "a.js");
            AssertRequire("../fixtures/b/c", "b/c.js");
            AssertRequire("../fixtures/b/d", "b/d.js");

            // Test loading relative paths from different files (adapted from require()s spread across files)
            Assert.AreEqual(Require("./fixtures/b/c.js"), NodeModuleService.ResolveModule("./b/c", Require("../fixtures/a")));
            Assert.AreEqual(Require("./fixtures/b/d.js"), NodeModuleService.ResolveModule("./d", Require("../fixtures/b/c")));
            Assert.AreEqual(Require("./fixtures/b/package/index.js"), NodeModuleService.ResolveModule("./package", Require("../fixtures/b/c")));

            // Absolute
            // I see no reason to support absolute paths.
            //Assert.AreEqual(Require("../fixtures/b/d"), Require(Path.Combine(SourceDirectory, "../fixtures/b/d")));

            // Adapted from test index.js modules ids and relative loading
            Assert.AreNotEqual(NodeModuleService.ResolveModule(Path.GetDirectoryName(Require("../fixtures/nested-index/two")), "./hello"),
                               NodeModuleService.ResolveModule(Path.GetDirectoryName(Require("../fixtures/nested-index/one")), "./hello")
                              );

            AssertRequire("../fixtures/empty", null);
        }

        [TestMethod]
        public void TrailingSlashTest()
        {
            // Adapted from test index.js in a folder with a trailing slash
            string three = Require("../fixtures/nested-index/three"),
                   threeFolder = Require("../fixtures/nested-index/three/"),
                   threeIndex = Require("../fixtures/nested-index/three/index.js");
            Assert.AreEqual(threeFolder, threeIndex);
            Assert.AreNotEqual(threeFolder, three);
        }

        [TestMethod]
        public void PackageJsonTest()
        {
            // Adapted from test package.json require() loading
            AssertRequire("../fixtures/packages/main", "packages/main/package-main-module.js", "Failed loading package");
            AssertRequire("../fixtures/packages/main-index", "packages/main-index/package-main-module/index.js", "Failed loading package with index.js in main subdir");
        }

        [TestMethod]
        public void ParentDirCyclesTest()
        {
            // Adapted from test cycles containing a .. path
            Assert.AreEqual(Require("./fixtures/cycles/folder/foo.js"), NodeModuleService.ResolveModule("./folder/foo", Require("../fixtures/cycles/root")));
            Assert.AreEqual(Require("./fixtures/cycles/root.js"), NodeModuleService.ResolveModule("./../root", Require("../fixtures/cycles/folder/foo")));
        }

        [TestMethod]
        public void NodeModulesTest()
        {
            var basePath = Path.Combine(TargetFixturesDir, "node_modules");

            // Custom Tests:
            Assert.AreEqual(Require("../fixtures/node_modules/baz/otherFile.js"), NodeModuleService.ResolveModule(basePath, "baz/otherFile"));

            // Imported from Node.js:

            // Adapted from test node_modules folders

            Assert.AreEqual(Require("../fixtures/node_modules/baz/index.js"), NodeModuleService.ResolveModule(basePath, "baz"));
            Assert.AreEqual(NodeModuleService.ResolveModule(basePath, "./baz/index.js"), NodeModuleService.ResolveModule(basePath, "baz"));

            basePath += @"\baz";
            Assert.AreEqual(NodeModuleService.ResolveModule(basePath, "../bar.js"), NodeModuleService.ResolveModule(basePath, "bar"));
            Assert.AreEqual(Require("../fixtures/node_modules/bar.js"), NodeModuleService.ResolveModule(basePath, "bar"));

            Assert.AreEqual(NodeModuleService.ResolveModule(basePath, "./node_modules/asdf.js"), NodeModuleService.ResolveModule(basePath, "asdf"));
            Assert.AreEqual(Require("../fixtures/node_modules/baz/node_modules/asdf"), NodeModuleService.ResolveModule(basePath, "asdf"));
        }
    }
}
