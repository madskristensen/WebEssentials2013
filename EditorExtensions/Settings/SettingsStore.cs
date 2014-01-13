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

        ///<summary>Loads the active settings file.</summary>
        public static void Load()
        {
            string jsonPath = GetFilePath();
            if (!File.Exists(jsonPath))
            {
                var legacyPath = jsonPath.Replace(FileName, _legacyFileName);
                if (File.Exists(legacyPath))
                {
                    new SettingsMigrator(legacyPath).ApplyTo(WESettings.Instance);
                    Save(jsonPath);
                    UpdateStatusBar("imported from legacy XML settings file");
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
            return Path.Combine(ssm.GetApplicationDataFolder(ApplicationDataFolder.Configuration), FileName);
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
