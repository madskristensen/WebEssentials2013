using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    internal static class ProjectHelpers
    {
        public static bool IsWebProject(this Project project)
        {
            // Web site project
            if (project.Kind.Equals("{E24C65DC-7377-472B-9ABA-BC803B73C61A}", StringComparison.OrdinalIgnoreCase))
                return true;

            // Check for Web Application projects.  See https://github.com/madskristensen/WebEssentials2013/pull/140#issuecomment-26679862
            return project.Properties.Item("WebApplication.UseIISExpress") != null;
        }

        public static string GetRootFolder(Project project = null)
        {
            try
            {
                EnvDTE80.DTE2 dte = EditorExtensionsPackage.DTE;
                Project activeProject = project ?? GetActiveProject();

                if (activeProject == null)
                {
                    var doc = dte.ActiveDocument;
                    if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                        return GetProjectFolder(doc.FullName);
                }

                if (activeProject == null)
                {
                    return string.Empty;
                }

                var fullPath = activeProject.Properties.Item("FullPath").Value;

                return fullPath == null ? string.Empty : fullPath.ToString();
            }
            catch (Exception)
            {
                //Logger.Log(ex);
                return string.Empty;
            }
        }

        internal static bool AddFileToActiveProject(string fileName, string itemType = null)
        {
            Project project = GetActiveProject();

            if (project != null)
            {
                string projectFilePath = project.Properties.Item("FullPath").Value.ToString();
                string projectDirPath = Path.GetDirectoryName(projectFilePath);

                if (fileName.StartsWith(projectDirPath, StringComparison.OrdinalIgnoreCase))
                {
                    ProjectItem item = project.ProjectItems.AddFromFile(fileName);

                    if (itemType != null && item != null && !project.FullName.Contains("://"))
                    {
                        try
                        {
                            item.Properties.Item("ItemType").Value = itemType;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return false;
        }

        public static Project GetActiveProject()
        {
            Project activeProject = null;

            try
            {
                Array activeSolutionProjects = EditorExtensionsPackage.DTE.ActiveSolutionProjects as Array;

                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                {
                    activeProject = activeSolutionProjects.GetValue(0) as Project;
                }
            }
            catch
            {
                Logger.Log("Error getting the active project");
            }

            return activeProject;
        }

        public static string ToAbsoluteFilePath(string relativeUrl, string relativeToFile)
        {
            var file = EditorExtensionsPackage.DTE.Solution.FindProjectItem(relativeToFile);
            return ToAbsoluteFilePath(relativeUrl, file);
        }

        public static string ToAbsoluteFilePathFromActiveFile(string relativeUrl)
        {
            return ToAbsoluteFilePath(relativeUrl, GetActiveFile());
        }

        public static string ToAbsoluteFilePath(string relativeUrl, ProjectItem file)
        {
            var projectFolder = GetProjectFolder(file);
            var project = GetProject(file);
            return ToAbsoluteFilePath(relativeUrl, project.Properties.Item("FullPath").Value.ToString(), projectFolder);
        }

        public static string ToAbsoluteFilePath(string relativeUrl, string projectRoot, string rootFolder)
        {
            string imageUrl = relativeUrl.Trim(new[] { '\'', '"' });
            var relUri = new Uri(imageUrl, UriKind.RelativeOrAbsolute);

            if (relUri.IsAbsoluteUri)
            {
                return relUri.LocalPath;
            }

            if (relUri.OriginalString.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).StartsWith(new string(Path.DirectorySeparatorChar, 1)))
            {
                rootFolder = null;
                relUri = new Uri(relUri.OriginalString.Substring(1), UriKind.Relative);
            }

            var root = (rootFolder ?? projectRoot).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (File.Exists(root))
            {
                root = Path.GetDirectoryName(root);
            }

            if (!root.EndsWith(new string(Path.DirectorySeparatorChar, 1)))
            {
                root += Path.DirectorySeparatorChar;
            }

            var rootUri = new Uri(root, UriKind.Absolute);

            try
            {
                return FixAbsolutePath(new Uri(rootUri, relUri).LocalPath);
            }
            catch (UriFormatException)
            {
                return string.Empty;
            }
        }

        public static ITextBuffer GetCurentTextBuffer()
        {
            return GetCurentTextView().TextBuffer;
        }

        public static IWpfTextView GetCurentTextView()
        {
            var componentModel = GetComponentModel();
            if (componentModel != null)
            {
                var editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                var textManager = (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));

                IVsTextView activeView = null;
                textManager.GetActiveView(1, null, out activeView);

                return editorAdapter.GetWpfTextView(activeView);
            }

            return null;
        }

        public static IComponentModel GetComponentModel()
        {
            return (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
        }

        public static IEnumerable<string> GetSelectedItemPaths()
        {
            var items = (Array)EditorExtensionsPackage.DTE.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (UIHierarchyItem selItem in items)
            {
                var item = selItem.Object as ProjectItem;
                if (item != null)
                {
                    yield return item.Properties.Item("FullPath").Value.ToString();
                }
            }
        }
        public static IEnumerable<Project> GetSelectedProjects()
        {
            var items = (Array)EditorExtensionsPackage.DTE.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (UIHierarchyItem selItem in items)
            {
                var item = selItem.Object as Project;
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        public static bool CheckOutFileFromSourceControl(string fileName)
        {
            try
            {
                var dte = EditorExtensionsPackage.DTE;

                if (File.Exists(fileName) && dte.Solution.FindProjectItem(fileName) != null)
                {
                    if (dte.SourceControl.IsItemUnderSCC(fileName) && !dte.SourceControl.IsItemCheckedOut(fileName))
                    {
                        dte.SourceControl.CheckOutItem(fileName);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            return false;
        }

        public static string GetSolutionFolderPath()
        {
            EnvDTE.Solution solution = EditorExtensionsPackage.DTE.Solution;

            if (solution == null || string.IsNullOrEmpty(solution.FullName))
                return null;

            return Path.GetDirectoryName(solution.FullName);
        }

        private static string GetProjectFolder(ProjectItem item)
        {
            if (item == null || item.ContainingProject == null || string.IsNullOrEmpty(item.ContainingProject.FullName)) // Solution items
                return null;

            var fullPath = item.ContainingProject.Properties.Item("FullPath").Value.ToString();

            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }

            if (File.Exists(fullPath))
            {
                return Path.GetDirectoryName(fullPath);
            }

            return string.Empty;
        }

        public static string GetProjectFolder(string fileNameOrFolder)
        {
            if (string.IsNullOrEmpty(fileNameOrFolder))
                return GetRootFolder();

            ProjectItem item = EditorExtensionsPackage.DTE.Solution.FindProjectItem(fileNameOrFolder);

            return GetProjectFolder(item);
        }

        public static IEnumerable<ProjectItem> GetSelectedItems()
        {
            var items = (Array)EditorExtensionsPackage.DTE.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (UIHierarchyItem selItem in items)
            {
                var item = selItem.Object as ProjectItem;
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        public static string FixAbsolutePath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return absolutePath;
            }

            var uniformlySeparated = absolutePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var doubleSlash = new string(Path.DirectorySeparatorChar, 2);
            var prependSeparator = uniformlySeparated.StartsWith(doubleSlash);
            uniformlySeparated = uniformlySeparated.Replace(doubleSlash, new string(Path.DirectorySeparatorChar, 1));

            if (prependSeparator)
            {
                uniformlySeparated = Path.DirectorySeparatorChar + uniformlySeparated;
            }

            return uniformlySeparated;
        }

        public static string GetActiveFilePath()
        {
            var doc = EditorExtensionsPackage.DTE.ActiveDocument;

            if (doc != null)
            {
                return ToAbsoluteFilePath(doc.FullName, doc.ProjectItem);
            }

            return string.Empty;
        }

        public static Project GetActiveFileProject()
        {
            var doc = EditorExtensionsPackage.DTE.ActiveDocument;

            if (doc != null)
            {
                return doc.ProjectItem.ContainingProject;
            }

            return null;
        }

        private static Project GetProject(ProjectItem projectItem)
        {
            if (projectItem == null)
            {
                return null;
            }

            return projectItem.ContainingProject;
        }

        public static Project GetProject(string item)
        {
            var projectItem = EditorExtensionsPackage.DTE.Solution.FindProjectItem(item);
            return GetProject(projectItem);
        }

        public static ProjectItem GetActiveFile()
        {
            var doc = EditorExtensionsPackage.DTE.ActiveDocument;

            if (doc != null)
            {
                return doc.ProjectItem;
            }

            return null;
        }
    }
}
