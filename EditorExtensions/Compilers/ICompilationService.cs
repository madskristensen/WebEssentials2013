using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Compilers
{
    ///<summary>Exposes compiled results from a source file to consumers.  This will initiate compilation automatically, as appropriate, and notify consumers when it's ready.</summary>
    interface ICompilationNotifier : IDisposable
    {
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
    interface ICompilationServiceProvider
    {
        ICompilationNotifier GetCompilationService(ITextDocument doc);
    }

    ///<summary>Allows components to automatically consume all compilations of a specific ContentType.</summary>
    interface ICompilationConsumer
    {
        ///<summary>Called when a file is compiled.  The source and target (may be null) filenames are available in the result.</summary>
        void OnCompiled(CompilerResult result);
    }
}
