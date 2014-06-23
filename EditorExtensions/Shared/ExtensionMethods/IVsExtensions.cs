using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public static class IVsExtensions
    {
        ///<summary>Gets a case-insensitive HashSet of all extensions for a given ContentType, including the leading dot.</summary>
        public static ISet<string> GetFileExtensionSet(this IFileExtensionRegistryService extService, IContentType contentType)
        {
            return new HashSet<string>(
                extService.GetExtensionsForContentType(contentType)
                    .Select(e => "." + e),
                StringComparer.OrdinalIgnoreCase
            );
        }

        public static void Execute(this EnvDTE.Commands command, Enum commandId, object arg = null)
        {
            command.Raise(commandId.GetType().GUID.ToString(), Convert.ToInt32(commandId, CultureInfo.InvariantCulture), arg, IntPtr.Zero);
        }

        public static void Execute(this IOleCommandTarget target, Enum commandId, IntPtr inHandle = default(IntPtr), IntPtr outHandle = default(IntPtr))
        {
            var c = commandId.GetType().GUID;
            ErrorHandler.ThrowOnFailure(target.Exec(ref c, Convert.ToUInt32(commandId, CultureInfo.InvariantCulture), 0, inHandle, outHandle));
        }

        const uint DISP_E_MEMBERNOTFOUND = 0x80020003;

        public static void AddHierarchyItem(this ErrorTask task)
        {
            IVsHierarchy hierarchyItem = null;
            IVsSolution solution = WebEssentialsPackage.GetGlobalService<IVsSolution>(typeof(SVsSolution));
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

        public static string GetFileName(this IPropertyOwner owner)
        {
            IVsTextBuffer bufferAdapter;

            if (!owner.Properties.TryGetProperty(typeof(IVsTextBuffer), out bufferAdapter))
                return null;

            var persistFileFormat = bufferAdapter as IPersistFileFormat;
            string ppzsFilename = null;
            uint pnFormatIndex;
            int returnCode = -1;

            if (persistFileFormat != null)
                try
                {
                    returnCode = persistFileFormat.GetCurFile(out ppzsFilename, out pnFormatIndex);
                }
                catch (NotImplementedException)
                {
                    return null;
                }

            if (returnCode != VSConstants.S_OK)
                return null;

            return ppzsFilename;
        }
    }
}
