using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests.IntegrationTests
{
    [TestClass]
    public class SettingsTests
    {
        [HostType("VS IDE")]
        [TestMethod]
        public async Task SolutionSettingsMigrationTest()
        {
            SettingsStore.InTestMode = false;   // Enable settings load
            File.Delete(Path.Combine(VSHost.FixtureDirectory, "LegacySettings", SettingsStore.FileName));

            VSHost.EnsureSolution(@"LegacySettings\LegacySettings.sln");
            await Task.Delay(750);  // Wait for things to load
            File.Exists(Path.Combine(VSHost.FixtureDirectory, "LegacySettings", SettingsStore.FileName))
                .Should().BeTrue("opening solution with legacy settings file should create new settings file");

            // Check some non-default values from the legacy XML
            WESettings.Instance.Less.CompileOnSave.Should().BeFalse();
            WESettings.Instance.TypeScript.BraceCompletion.Should().BeFalse();
            WESettings.Instance.TypeScript.LintOnSave.Should().BeFalse();

            // Check default values for new settings
            WESettings.Instance.Html.GzipMinifiedFiles.Should().BeFalse();
            WESettings.Instance.Less.EnableChainCompilation.Should().BeTrue();
        }
    }
}
