using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace WebEssentialsTests.IntegrationTests
{
    [TestClass]
    public class SettingsTests
    {
        [HostType("VS IDE")]
        [TestMethod]
        public void SolutionSettingsMigrationTest()
        {
            SettingsStore.InTestMode = false;   // Enable settings load
            File.Delete(Path.Combine(VSHost.FixtureDirectory, "LegacySettings", SettingsStore.FileName));
            //Debug.Assert(false);
            VSHost.EnsureSolution(@"LegacySettings\LegacySettings.sln");
            File.Exists(Path.Combine(VSHost.FixtureDirectory, "LegacySettings", SettingsStore.FileName))
                .Should().BeTrue("opening solution with legacy settings file should create new settings file");

            // Check some non-default values from the legacy XML
            WESettings.Instance.Less.CompileOnSave.Should().BeFalse();
            WESettings.Instance.TypeScript.BraceCompletion.Should().BeFalse();
            WESettings.Instance.TypeScript.LintOnSave.Should().BeFalse();

            // Check default values for new settings
            WESettings.Instance.Html.GzipMinifiedFiles.Should().BeFalse();
            WESettings.Instance.Markdown.CompileOnBuild.Should().BeTrue();
        }
    }
}
