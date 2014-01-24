using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public static class IVsExtensions
    {
        ///<summary>Gets a case-insensitive HashSet of all extensions for a given ContentType, including the leading dot.</summary>
        public static ISet<string> GetFileExtensionSet(this IFileExtensionRegistryService fers, IContentType contentType)
        {
            return new HashSet<string>(
                fers.GetExtensionsForContentType(contentType)
                    .Select(e => "." + e),
                StringComparer.OrdinalIgnoreCase
            );
        }

        public static void Execute(this EnvDTE.Commands c, Enum commandId, object arg = null)
        {
            c.Raise(commandId.GetType().GUID.ToString(), Convert.ToInt32(commandId), arg, IntPtr.Zero);
        }
        public static void Execute(this IOleCommandTarget t, Enum commandId, IntPtr inVar = default(IntPtr), IntPtr outVar = default(IntPtr))
        {
            var c = commandId.GetType().GUID;
            t.Exec(ref c, Convert.ToUInt32(commandId), 0, inVar, outVar);
        }

        const uint DISP_E_MEMBERNOTFOUND = 0x80020003;

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
