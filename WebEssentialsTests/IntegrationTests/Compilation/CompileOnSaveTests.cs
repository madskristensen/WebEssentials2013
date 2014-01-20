using System;
using System.IO;
using System.Threading.Tasks;
using EnvDTE;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace WebEssentialsTests.IntegrationTests.Compilation
{
    [TestClass]
    public class CompileOnSaveTests
    {
        static DTE DTE { get { return VsIdeTestHostContext.Dte; } }

        static async Task WaitFor(Func<bool> test, int maxSeconds)
        {
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalSeconds < maxSeconds)
            {
                if (test())
                    return;
                await Task.Delay(250);
            }
            Assert.Fail("Timed out waiting");
        }

        [HostType("VS IDE")]
        [TestMethod]
        public async Task CompileLessOnSaveWithoutProject()
        {
            //System.Diagnostics.Debugger.Launch();
            SettingsStore.EnterTestMode();
            var fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".less");

            try
            {
                File.WriteAllText(fileName, @"a{b{color:red;}}");
                DTE.ItemOperations.OpenFile(fileName).Document.Save();
                await WaitFor(() => File.Exists(Path.ChangeExtension(fileName, ".css")), 10);
            }
            finally
            {
                File.Delete(fileName);
                File.Delete(Path.ChangeExtension(fileName, ".css"));
            }
        }
    }
}
