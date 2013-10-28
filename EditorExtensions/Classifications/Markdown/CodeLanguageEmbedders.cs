using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Html.Editor.Projection;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    ///<summary>Preprocesses embedded code Markdown blocks before creating projection buffers.</summary>
    ///<remarks>
    /// Implement this interface to initialize language services for
    /// your language, or to add custom wrapping text around blocks.
    /// Implementations should be state-less; only one instance will
    /// be created.
    ///</remarks>
    public interface ICodeLanguageEmbedder
    {
        ///<summary>Gets text to insert around an embedded code block for this language.</summary>
        ///<param name="code">The lines of code in the block.  Enumerating this may be expensive.</param>
        ///<returns>
        /// One of the following:
        ///  - Null or an empty sequence to surround with \r\n.
        ///  - A single string to put on both ends of the code.
        ///  - Two strings; one for each end of the code block.
        /// The buffer generator will always add newlines.
        ///</returns>
        IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code);

        ///<summary>Called when a block of this type is first created within a document.</summary>
        void OnBlockCreated(ITextBuffer editorBuffer, LanguageProjectionBuffer projectionBuffer);

        ///<summary>Called when the user enters a block of this type.</summary>
        void OnBlockEntered();
    }

    [Export(typeof(ICodeLanguageEmbedder))]
    [ContentType("CSS")]
    class CssEmbedder : ICodeLanguageEmbedder
    {
        public IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code)
        {
            // If the code doesn't have any braces, surround it in a ruleset so that properties are valid.
            if (code.All(t => t.IndexOfAny(new[] { '{', '}' }) == -1))
                return new[] { ".GeneratedClass-" + Guid.NewGuid() + " {", "}" };
            return null;
        }

        public void OnBlockCreated(ITextBuffer editorBuffer, LanguageProjectionBuffer projectionBuffer) { }
        public void OnBlockEntered() { }
    }
    [Export(typeof(ICodeLanguageEmbedder))]
    [ContentType("Javascript")]
    class JavascriptEmbedder : ICodeLanguageEmbedder
    {
        // Statements like return or arguments can only appear inside a function.
        // There are no statements that cannot appear in a function.
        // TODO: IntelliSense for Node.js vs. HTML.
        static readonly IReadOnlyCollection<string> wrapper = new[] { "function() {", "}" };
        public IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code) { return wrapper; }
        public void OnBlockCreated(ITextBuffer editorBuffer, LanguageProjectionBuffer projectionBuffer) { }
        public void OnBlockEntered() { }
    }

    abstract class IntellisenseProjectEmbedder : ICodeLanguageEmbedder
    {
        public abstract IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code);
        public abstract string LanguageServiceName { get; }

        public void OnBlockCreated(ITextBuffer editorBuffer, LanguageProjectionBuffer projectionBuffer)
        {
            ContainedLanguageAdapter.ForBuffer(editorBuffer).AddIntellisenseProjectLanguage(projectionBuffer, LanguageServiceName, false);
        }

        public void OnBlockEntered()
        {
        }
    }

    [Export(typeof(ICodeLanguageEmbedder))]
    [ContentType("CSharp")]
    class CSharpEmbedder : IntellisenseProjectEmbedder
    {
        public override string LanguageServiceName { get { return "C#"; } }
        public override IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code)
        {
            return new[] { @"
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

partial class Entry { object SampleMethod" + Guid.NewGuid().ToString("n") + @"() {", "}}" };
        }
    }

    [Export(typeof(ICodeLanguageEmbedder))]
    [ContentType("Basic")]
    class VBEmbedder : IntellisenseProjectEmbedder
    {
        public override string LanguageServiceName { get { return "VB"; } }
        public override IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code)
        {
            return new[] { @"Partial Class Entry
Function SampleMethod" + Guid.NewGuid().ToString("n") + @"()", @"
End Function
End Class" };
        }
    }
}
