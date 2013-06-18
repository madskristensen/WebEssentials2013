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
        public static string GetRootFolder()
        {
            try
            {
                EnvDTE80.DTE2 dte = EditorExtensionsPackage.DTE;
                Project activeProject = null;

                if (dte.Solution.Projects.Count == 1 && !string.IsNullOrEmpty(dte.Solution.Projects.Item(1).FullName))
                {
                    return dte.Solution.Projects.Item(1).Properties.Item("FullPath").Value.ToString();
                }

                Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;

                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                {
                    activeProject = activeSolutionProjects.GetValue(0) as Project;
                }

                return activeProject.Properties.Item("FullPath").Value.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
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
                        catch { }
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

        public static string ToAbsoluteFilePath(string relativeUrl, string rootFolder = null)
        {
            string imageUrl = relativeUrl.Trim(new[] { '\'', '"' });
            string filePath = string.Empty;

            if (imageUrl.StartsWith("/", StringComparison.Ordinal))
            {
                string root = rootFolder ?? ProjectHelpers.GetRootFolder();

                if (root.Contains("://"))
                {
                    filePath = root + imageUrl;
                }
                else if (!string.IsNullOrEmpty(root))
                {
                    if (!Directory.Exists(root))
                    {
                        filePath = new FileInfo(root).Directory + imageUrl;
                    }
                    else
                    {
                        return root + imageUrl.Replace("/", "\\");
                    }
                }
            }
            else
            {
                FileInfo fi = new FileInfo(EditorExtensionsPackage.DTE.ActiveDocument.FullName);
                DirectoryInfo dir = fi.Directory;

                while (imageUrl.Contains("../"))
                {
                    imageUrl = imageUrl.Remove(imageUrl.IndexOf("../", StringComparison.Ordinal), 3);
                    dir = dir.Parent;
                }

                filePath = Path.Combine(dir.FullName, imageUrl.Replace("/", "\\"));
            }

            return filePath;
        }

        public static ITextBuffer GetCurentTextBuffer()
        {
            return GetCurentTextView().TextBuffer;
        }

        public static IWpfTextView GetCurentTextView()
        {
            var componentModel = GetComponentModel();
            var editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            var textManager = (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));

            IVsTextView activeView = null;
            textManager.GetActiveView(1, null, out activeView);

            return editorAdapter.GetWpfTextView(activeView);
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

        public static string GetProjectFolder(string fileNameOrFolder)
        {
            if (string.IsNullOrEmpty(fileNameOrFolder))
                return GetRootFolder();

            ProjectItem item = EditorExtensionsPackage.DTE.Solution.FindProjectItem(fileNameOrFolder);

            if (item == null || item.ContainingProject == null || string.IsNullOrEmpty(item.ContainingProject.FullName)) // Solution items
                return null;

            return item.ContainingProject.Properties.Item("FullPath").Value.ToString();
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
    }
}
