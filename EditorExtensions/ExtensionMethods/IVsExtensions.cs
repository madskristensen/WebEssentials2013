using System.Collections.Generic;
using EnvDTE;
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.EditorExtensions
{
    public static class IVsExtensions
    {
        public static void AddHierarchyItem(this ErrorTask task)
        {
            IVsHierarchy HierarchyItem;
            IVsSolution solution = EditorExtensionsPackage.GetGlobalService<IVsSolution>(typeof(SVsSolution));
            Project project = ProjectHelpers.GetActiveProject();

            if (solution != null && project != null)
            {
                int flag = solution.GetProjectOfUniqueName(project.FullName, out HierarchyItem);

                if (0 == flag)
                {
                    task.HierarchyItem = HierarchyItem;
                }
            }
        }

        public static bool IsLink(this ProjectItem item)
        {
            try
            {
                return (bool)item.Properties.Item("IsLink").Value;
            }
            catch
            {
                return false;
            }
        }

        ///<summary>Sets the Description property for a set of completion items.</summary>
        ///<remarks>This method is used to help build completion lists without repetition.</remarks>
        public static IList<T> WithDescription<T>(this IList<T> completions, string tooltip) where T : Microsoft.VisualStudio.Language.Intellisense.Completion
        {
            foreach (var c in completions)
            {
                c.Description = tooltip;
            }
            return completions;
        }
        ///<summary>Sets the Description property for a set of completion items.</summary>
        ///<remarks>This method is used to help build completion lists without repetition.</remarks>
        public static IList<T> WithDescription<T>(this IList<T> completions, Func<T, string> tooltipGenerator) where T : Microsoft.VisualStudio.Language.Intellisense.Completion
        {
            foreach (var c in completions)
            {
                c.Description = tooltipGenerator(c);
            }
            return completions;
        }
    }
}
