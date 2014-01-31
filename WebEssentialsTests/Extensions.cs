using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.Web.Editor;

namespace WebEssentialsTests
{
    static class Extensions
    {
        ///<summary>Gets the current text in a <see cref="ITextView"/>.</summary>
        public static string GetText(this ITextView textView) { return textView.TextBuffer.CurrentSnapshot.GetText(); }

        public static bool IsCompletionOpen(this ITextView textView)
        {
            return WebEditor.ExportProvider.GetExport<ICompletionBroker>().Value.IsCompletionActive(textView);
        }


        ///<summary>Compiles an existing file on disk to a string.</summary>
        public static async Task<string> CompileToStringAsync(this NodeExecutorBase compiler, string sourceFileName)
        {
            // CoffeeScript cannot compile to a different filename.
            var targetFileName = Path.ChangeExtension(sourceFileName, compiler.TargetExtension);

            try
            {
                var result = await compiler.CompileAsync(sourceFileName, targetFileName);

                if (result.IsSuccess)
                    return result.Result;
                else
                    throw new ExternalException(result.Errors.First().Message);
            }
            finally
            {
                File.Delete(targetFileName);
            }
        }
        ///<summary>Compiles an in-memory string.</summary>
        public static async Task<string> CompileSourceAsync(this NodeExecutorBase compiler, string source, string sourceExtension)
        {
            var sourceFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + sourceExtension);
            try
            {
                File.WriteAllText(sourceFileName, source);
                return await compiler.CompileToStringAsync(sourceFileName);
            }
            finally
            {
                File.Delete(sourceFileName);
            }
        }
    }
}
