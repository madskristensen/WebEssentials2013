using System.Collections.Generic;
using System.Runtime.InteropServices;
using EnvDTE;
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.EditorExtensions
{
    public static class IVsExtensions
    {
        public const uint DISP_E_MEMBERNOTFOUND = 0x80020003;

        public static void AddHierarchyItem(this ErrorTask task)
        {
            IVsHierarchy hierarchyItem = null;
            IVsSolution solution = EditorExtensionsPackage.GetGlobalService<IVsSolution>(typeof(SVsSolution));
            Project project = ProjectHelpers.GetActiveProject();

            if (solution != null && project != null)
            {
                int flag = -1;

                try
                {
                    flag = solution.GetProjectOfUniqueName(project.FullName, out hierarchyItem);
                }
                catch (COMException ex)
                {
                    if ((uint)ex.ErrorCode != DISP_E_MEMBERNOTFOUND)
                    {
                        throw;
                    }
                }

                if (0 == flag)
                {
                    task.HierarchyItem = hierarchyItem;
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
