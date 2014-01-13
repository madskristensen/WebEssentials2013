using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ConfOxide;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Keys = MadsKristensen.EditorExtensions.WESettings.Keys;

namespace MadsKristensen.EditorExtensions
{
    internal static class SettingsStore
    {
        public const string _legacyFileName = "WE2013-settings.xml";
        public const string _fileName = "WebEssentials-Settings.json";
        public const string _solutionFolder = "Solution Items";

        public static bool SolutionSettingsExist
        {
            get { return File.Exists(GetSolutionFilePath()); }
        }

        ///<summary>Loads the active settings file.</summary>
        public static void Load()
        {
            WESettings.Instance.ReadJsonFile(GetFilePath());
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

            Solution2 solution = EditorExtensionsPackage.DTE.Solution as Solution2;
            Project project = solution.Projects
                                .OfType<Project>()
                                .FirstOrDefault(p => p.Name.Equals(_solutionFolder, StringComparison.OrdinalIgnoreCase))
                           ?? solution.AddSolutionFolder(_solutionFolder);

            project.ProjectItems.AddFromFile(path);
            UpdateStatusBar("applied");
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

            return Path.Combine(Path.GetDirectoryName(solution.FullName), _fileName);
        }

        private static string GetUserFilePath()
        {
            var ssm = new ShellSettingsManager(EditorExtensionsPackage.Instance);
            return Path.Combine(ssm.GetApplicationDataFolder(ApplicationDataFolder.Configuration), _fileName);
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
