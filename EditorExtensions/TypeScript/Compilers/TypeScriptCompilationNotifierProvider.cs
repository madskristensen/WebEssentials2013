using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Compilers;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.TypeScript
{
    [Export(typeof(ICompilationNotifierProvider))]
    [ContentType("TypeScript")]
    class TypeScriptCompilerNotifierProvider : ICompilationNotifierProvider
    {
        public ICompilationNotifier GetCompilationNotifier(ITextDocument doc)
        {
            return new TypeScriptCompilationNotifier(doc);
        }
    }
}