using System;
using System.IO;
using System.Linq;
using ConfOxide;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace MadsKristensen.EditorExtensions
{
    internal static class SettingsStore
    {
        const string _legacyFileName = "WE2013-settings.xml";
        public const string FileName = "WebEssentials-Settings.json";

        public static bool SolutionSettingsExist
        {
            get { return File.Exists(GetSolutionFilePath()); }
        }

        public static bool InTestMode { get; set; }
        ///<summary>Resets all settings and disables persistence for unit tests.</summary>
        /// <param name="testSettings">Optional settings to apply for the tests.</param>
        /// <remarks>It is completely safe to call this function multiple times.</remarks>
        public static void EnterTestMode(WESettings testSettings = null)
        {
            InTestMode = true;
            if (testSettings != null)
                WESettings.Instance.AssignFrom(testSettings);
            else
                WESettings.Instance.ResetValues();
        }

        ///<summary>Loads the active settings file.</summary>
        public static void Load()
        {
            if (InTestMode) return;
            string jsonPath = GetFilePath();
            if (!File.Exists(jsonPath))
            {
                var legacyPath = GetLegacyFilePath();
                if (File.Exists(legacyPath))
                {
                    new SettingsMigrator(legacyPath).ApplyTo(WESettings.Instance);
                    Save(jsonPath);
                    ProjectHelpers.GetSolutionItemsProject().ProjectItems.AddFromFile(jsonPath);
                    UpdateStatusBar("imported from legacy XML settings file");
                    Logger.Log("Migrated legacy XML settings file " + legacyPath + " to " + jsonPath);
                }
                return;
            }

            WESettings.Instance.ReadJsonFile(jsonPath);
            UpdateStatusBar("applied");
        }

        ///<summary>Saves the current settings to the active settings file.</summary>
        public static void Save() { Save(GetFilePath()); }
        ///<summary>Saves the current settings to the specified settings file.</summary>
        private static void Save(string filename)
        {
            ProjectHelpers.CheckOutFileFromSourceControl(filename);
            WESettings.Instance.WriteJsonFile(filename);
            UpdateStatusBar("updated");
        }

        ///<summary>Creates a settings file for the active solution if one does not exist already, initialized from the current settings.</summary>
        public static void CreateSolutionSettings()
        {
            string path = GetSolutionFilePath();
            if (File.Exists(path))
                return;

            Save(path);
            ProjectHelpers.GetSolutionItemsProject().ProjectItems.AddFromFile(path);
            UpdateStatusBar("created");
        }

        private static string GetFilePath()
        {
            string path = GetSolutionFilePath();

            if (!File.Exists(path))
                path = GetUserFilePath();

            return path;
        }
        private static string GetLegacyFilePath()
        {
            string path = (GetSolutionFilePath() ?? "").Replace(FileName, _legacyFileName);

            if (!File.Exists(path))
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Web Essentials", _legacyFileName);

            return path;
        }

        public static string GetSolutionFilePath()
        {
            Solution solution = EditorExtensionsPackage.DTE.Solution;

            if (solution == null || string.IsNullOrEmpty(solution.FullName))
                return null;

            return Path.Combine(Path.GetDirectoryName(solution.FullName), FileName);
        }

        private static string GetUserFilePath()
        {
            var ssm = new ShellSettingsManager(EditorExtensionsPackage.Instance);
            return Path.Combine(ssm.GetApplicationDataFolder(ApplicationDataFolder.RoamingSettings), FileName);
        }

        public static void UpdateStatusBar(string action)
        {
            try
            {
                if (SolutionSettingsExist)
                    EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Solution settings " + action;
                else
                    EditorExtensionsPackage.DTE.StatusBar.Text = "Web Essentials: Global settings " + action;
            }
            catch
            {
                Logger.Log("Error updating status bar");
            }
        }
    }
}
