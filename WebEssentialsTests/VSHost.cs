using System;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace WebEssentialsTests
{
    public static class VSHost
    {
        static readonly string BaseDirectory = Path.GetDirectoryName(typeof(VSHost).Assembly.Location);
        public static readonly string FixtureDirectory = Path.Combine(BaseDirectory, "fixtures", "Visual Studio");

        public static DTE DTE { get { return VsIdeTestHostContext.Dte; } }
        public static IServiceProvider ServiceProvider { get { return VsIdeTestHostContext.ServiceProvider; } }

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

        public static void TypeString(string s)
        {
            foreach (var ch in s) TypeChar(ch);
        }

        ///<summary>Sends a single keypress command to Visual Studio.</summary>
        public static void TypeChar(char c)
        {
            // TODO: Register filter in active IVsTextView?
            var special = GetSpecialCommand(c);
            if (special != null)
            {
                DTE.Commands.Execute(special);
                return;
            }
            // Thanks @JaredPar
            var variantIn = Marshal.AllocCoTaskMem(32); // size of(VARIANT), 16 may be enough
            VariantInit(variantIn);
            try
            {
                Marshal.GetNativeVariantForObject(c, variantIn);
                DTE.Commands.Execute(VSConstants.VSStd2KCmdID.TYPECHAR, variantIn);
            }
            finally
            {
                VariantClear(variantIn);
                Marshal.FreeCoTaskMem(variantIn);
            }

        }
        [DllImport("oleaut32")]
        internal static extern void VariantClear(IntPtr variant);
        [DllImport("oleaut32")]
        private static extern void VariantInit(IntPtr pObject);

        // https://github.com/jaredpar/VsVim/blob/31d9222647ee8008808b8002ab64f4c4230fb81c/Src/VsVimShared/OleCommandUtil.cs#L330
        private static VSConstants.VSStd2KCmdID? GetSpecialCommand(char c)
        {
            switch (c)
            {
                case '\n':
                    return VSConstants.VSStd2KCmdID.RETURN;
                case '\t':
                    return VSConstants.VSStd2KCmdID.TAB;
                //? VSConstants.VSStd2KCmdID.BACKTAB
                //: VSConstants.VSStd2KCmdID.TAB;
                //TODO: Use ConsoleKey enum for other keys?
                //case VimKey.Escape:
                //    return VSConstants.VSStd2KCmdID.CANCEL;
                //case VimKey.Delete:
                //    return VSConstants.VSStd2KCmdID.DELETE;
                //case VimKey.Back:
                //    return VSConstants.VSStd2KCmdID.BACKSPACE;
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