using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using EnvDTE;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using Task = System.Threading.Tasks.Task;
using WebEditor = Microsoft.Web.Editor.WebEditor;

namespace WebEssentialsTests
{
    public static class VSHost
    {
        static readonly string BaseDirectory = Path.GetDirectoryName(typeof(VSHost).Assembly.Location);
        public static readonly string FixtureDirectory = Path.Combine(BaseDirectory, "fixtures", "Visual Studio");

        public static DTE DTE { get { return VsIdeTestHostContext.Dte; } }
        public static System.IServiceProvider ServiceProvider { get { return VsIdeTestHostContext.ServiceProvider; } }

        public static T GetService<T>(Type idType) { return (T)ServiceProvider.GetService(idType); }

        ///<summary>Ensures that the specified solution is open.</summary>
        ///<param name="relativePath">The path to the solution file, relative to fixtures\Visual Studio.</param>
        public static Solution EnsureSolution(string relativePath)
        {
            var fileName = Path.GetFullPath(Path.Combine(FixtureDirectory, relativePath));
            if (!File.Exists(fileName))
                throw new FileNotFoundException("Solution file does not exist", fileName);
            var solution = VsIdeTestHostContext.Dte.Solution;
            if (solution.FullName != fileName)
                solution.Open(fileName);
            return solution;
        }

        public static async Task<IWpfTextView> TypeText(string extension, string keystrokes)
        {
            DTE.ItemOperations.NewFile(Name: Guid.NewGuid() + "." + extension.TrimStart('.'));
            await TypeString(keystrokes);
            return ProjectHelpers.GetCurentTextView();
        }

        public static Task TypeString(string s)
        {
            // Wait for ApplicationIdle to make sure that all targets have been registered
            return Dispatcher.InvokeAsync(() => TypeChars(s), DispatcherPriority.ApplicationIdle).Task;
        }

        public static Dispatcher Dispatcher
        {
            get { return Dispatcher.FromThread(WebEditor.UIThread); }
        }

        ///<summary>Sends a series of keypress command to Visual Studio.</summary>
        public static void TypeChars(string s)
        {
            var target = (IOleCommandTarget)ProjectHelpers.GetCurrentNativeTextView();

            IntPtr variantIn = IntPtr.Zero;
            try
            {
                // Thanks @JaredPar
                variantIn = Marshal.AllocCoTaskMem(16); // size of(VARIANT)
                NativeMethods.VariantInit(variantIn);

                foreach (var c in s)
                {
                    var special = GetSpecialCommand(c);
                    if (special != null)
                    {
                        target.Execute(special.Value);
                    }
                    else
                    {
                        Marshal.GetNativeVariantForObject(c, variantIn);
                        target.Execute(VSConstants.VSStd2KCmdID.TYPECHAR, variantIn);
                    }
                }
            }
            finally
            {
                if (variantIn != IntPtr.Zero)
                {
                    NativeMethods.VariantClear(variantIn);
                    Marshal.FreeCoTaskMem(variantIn);
                }
            }
        }

        internal static class NativeMethods
        {
            [DllImport("oleaut32", CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.I4)]
            internal static extern Int32 VariantClear(IntPtr variant);

            [DllImport("oleaut32", CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.AsAny)]
            internal static extern void VariantInit(IntPtr pObject);
        }

        // https://github.com/jaredpar/VsVim/blob/31d9222647ee8008808b8002ab64f4c4230fb81c/Src/VsVimShared/OleCommandUtil.cs#L330
        private static VSConstants.VSStd2KCmdID? GetSpecialCommand(char c)
        {
            switch (c)
            {
                case '\n':
                    return VSConstants.VSStd2KCmdID.RETURN;
                case '\b':
                    return VSConstants.VSStd2KCmdID.BACKSPACE;
                case '\t':
                    return VSConstants.VSStd2KCmdID.TAB;
                //? VSConstants.VSStd2KCmdID.BACKTAB
                //: VSConstants.VSStd2KCmdID.TAB;
                //TODO: Use ConsoleKey enum for other keys?
                //case VimKey.Escape:
                //    return VSConstants.VSStd2KCmdID.CANCEL;
                //case VimKey.Delete:
                //    return VSConstants.VSStd2KCmdID.DELETE;
                //case VimKey.Up:
                //    return simulateStandardKeyBindings && hasShift
                //        ? VSConstants.VSStd2KCmdID.UP_EXT
                //        : VSConstants.VSStd2KCmdID.UP;
                //case VimKey.Down:
                //    return simulateStandardKeyBindings && hasShift
                //        ? VSConstants.VSStd2KCmdID.DOWN_EXT
                //        : VSConstants.VSStd2KCmdID.DOWN;
                //case VimKey.Left:
                //    return simulateStandardKeyBindings && hasShift
                //        ? VSConstants.VSStd2KCmdID.LEFT_EXT
                //        : VSConstants.VSStd2KCmdID.LEFT;
                //case VimKey.Right:
                //    return simulateStandardKeyBindings && hasShift
                //        ? VSConstants.VSStd2KCmdID.RIGHT_EXT
                //        : VSConstants.VSStd2KCmdID.RIGHT;
                //case VimKey.PageUp:
                //    return simulateStandardKeyBindings && hasShift
                //        ? VSConstants.VSStd2KCmdID.PAGEUP_EXT
                //        : VSConstants.VSStd2KCmdID.PAGEUP;
                //case VimKey.PageDown:
                //    return simulateStandardKeyBindings && hasShift
                //        ? VSConstants.VSStd2KCmdID.PAGEDN_EXT
                //        : VSConstants.VSStd2KCmdID.PAGEDN;
                //case VimKey.Insert:
                //    return VSConstants.VSStd2KCmdID.TOGGLE_OVERTYPE_MODE;
                default:
                    return null;
            }
        }
    }
}