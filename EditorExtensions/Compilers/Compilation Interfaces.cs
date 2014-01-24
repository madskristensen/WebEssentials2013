using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Compilers
{
    ///<summary>Exposes compiled results from a source file to consumers.  This will initiate compilation automatically, as appropriate, and notify consumers when it's ready.</summary>
    public interface ICompilationNotifier : IDisposable
    {
        ///<summary>
        /// Requests that the notifier raise <see cref="CompilationReady"/> with the last available result.  
        /// The notifier may ignore the request, raise the event later, or raise it re-entrantly.</summary>
        ///<param name="cached">
        /// If true, the compiler may return cached output from an older compilation;
        /// if false, the compiler should try to recompile if possible.  
        /// The implementation may ignore this parameter.
        /// Either way, no result will be saved to disk.
        ///</param>
        void RequestCompilationResult(bool cached);
        ///<summary>Raised whenever new compiled output is available.</summary>
        event EventHandler<CompilerResultEventArgs> CompilationReady;
    }
    ///<summary>Provides data for CompilerResult events.</summary>
    public class CompilerResultEventArgs : EventArgs
    {
        ///<summary>Creates a new CompilerResultEventArgs instance.</summary>
        public CompilerResultEventArgs(CompilerResult result) { CompilerResult = result; }

        ///<summary>Gets the result.</summary>
        public CompilerResult CompilerResult { get; private set; }
    }

    ///<summary>Creates <see cref="ICompilationNotifier"/> implementations for a specific content type.</summary>
    interface ICompilationNotifierProvider
    {
        ICompilationNotifier GetCompilationNotifier(ITextDocument doc);
    }
    ///<summary>Creates <see cref="CompilerRunnerBase"/> implementations for a specific content type.</summary>
    interface ICompilerRunnerProvider
    {
        CompilerRunnerBase GetCompiler(IContentType contentType);
    }
}
