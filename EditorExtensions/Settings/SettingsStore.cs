using System;
using System.IO;
using ConfOxide;
using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace MadsKristensen.EditorExtensions.Settings
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

            if (!File.Exists(GetSolutionFilePath()))
            {
                // If there is a legacy solution settings file, and no
                // modern solution settings file, migrate it.
                if (File.Exists(GetLegacySolutionFilePath()))
                {
                    Migrate(solution: true);
                    return;
                }
                // If there aren't any solution-level settings, check
                // whether we need to migrate legacy global settings.
                if (File.Exists(GetLegacyFilePath()) && !File.Exists(GetUserFilePath()))
                {
                    Migrate(solution: false);
                    return;
                }
            }

            string jsonPath = GetFilePath();
            if (File.Exists(jsonPath))
            {
                WESettings.Instance.ReadJsonFile(jsonPath);
                UpdateStatusBar("applied");
            }
        }

        private static void Migrate(bool solution)
        {
            var legacyPath = GetLegacyFilePath();
            if (!File.Exists(legacyPath))
                return;

            new SettingsMigrator(legacyPath).ApplyTo(WESettings.Instance);

            var jsonPath = solution ? GetSolutionFilePath() : GetUserFilePath();
            Save(jsonPath);
            if (solution)
                ProjectHelpers.GetSolutionItemsProject().ProjectItems.AddFromFile(jsonPath);

            UpdateStatusBar("imported from legacy XML settings file");
            Logger.Log("Migrated legacy XML settings file " + legacyPath + " to " + jsonPath);
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

        #region Legacy Locator
        private static string GetLegacyFilePath()
        {
            string path = (GetSolutionFilePath() ?? "").Replace(FileName, _legacyFileName);

            if (!File.Exists(path))
                return GetLegacyUserFilePath();
            return path;
        }
        private static string GetLegacySolutionFilePath()
        {
            string path = GetSolutionFilePath();
            if (string.IsNullOrEmpty(path))
                return null;
            return path.Replace(FileName, _legacyFileName);
        }
        private static string GetLegacyUserFilePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Web Essentials", _legacyFileName);
        }
        #endregion

        #region Modern Locator
        private static string GetFilePath()
        {
            string path = GetSolutionFilePath();

            if (!File.Exists(path))
                path = GetUserFilePath();

            return path;
        }
        public static string GetSolutionFilePath()
        {
            Solution solution = WebEssentialsPackage.DTE.Solution;

            if (solution == null || string.IsNullOrEmpty(solution.FullName))
                return null;

            return Path.Combine(Path.GetDirectoryName(solution.FullName), FileName);
        }
        private static string GetUserFilePath()
        {
            var ssm = new ShellSettingsManager(WebEssentialsPackage.Instance);
            return Path.Combine(ssm.GetApplicationDataFolder(ApplicationDataFolder.RoamingSettings), FileName);
        }
        #endregion

        public static void UpdateStatusBar(string action)
        {
            try
            {
                if (SolutionSettingsExist)
                    WebEssentialsPackage.DTE.StatusBar.Text = "Web Essentials: Solution settings " + action;
                else
                    WebEssentialsPackage.DTE.StatusBar.Text = "Web Essentials: Global settings " + action;
            }
            catch
            {
                Logger.Log("Error updating status bar");
            }
        }
    }
}
