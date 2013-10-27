using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        void OnBlockCreated();

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

        public void OnBlockCreated() { }
        public void OnBlockEntered() { }
    }
    [Export(typeof(ICodeLanguageEmbedder))]
    [ContentType("Javascript")]
    class JavascriptEmbedder : ICodeLanguageEmbedder
    {
        // Statements like return or arguments can only appear inside a function.
        // There are no statements that cannot appear in a function.
        // TODO: IntelliSense for Node.js vs. HTML.
        static readonly IReadOnlyCollection<string> wrapper = new[] { "function() {\r\n", "\r\n}" };
        public IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code) { return wrapper; }
        public void OnBlockCreated() { }
        public void OnBlockEntered() { }
    }
}
