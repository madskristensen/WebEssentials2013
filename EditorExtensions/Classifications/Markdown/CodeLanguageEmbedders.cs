using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Html.Editor.Projection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Win32;

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
        ///<summary>Gets a string to insert at the top of the generated ProjectionBuffr for this language.</summary>
        ///<remarks>Each Markdown file will have exactly one copy of this string in its code buffer.</remarks>
        string GlobalPrefix { get; }
        ///<summary>Gets a string to insert at the bottom of the generated ProjectionBuffr for this language.</summary>
        ///<remarks>Each Markdown file will have exactly one copy of this string in its code buffer.</remarks>
        string GlobalSuffix { get; }

        ///<summary>Gets text to insert around each embedded code block for this language.</summary>
        ///<param name="code">The lines of code in the block.  Enumerating this may be expensive.</param>
        ///<returns>
        /// One of the following:
        ///  - Null or an empty sequence to surround with \r\n.
        ///  - A single string to put on both ends of the code.
        ///  - Two strings; one for each end of the code block.
        /// The buffer generator will always add newlines.
        ///</returns>
        ///<remarks>
        /// These strings will be wrapped around every embedded
        /// code block separately.
        ///</remarks>
        IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code);

        ///<summary>Called when a block of this type is first created within a document.</summary>
        void OnBlockCreated(ITextBuffer editorBuffer, LanguageProjectionBuffer projectionBuffer);
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
        public string GlobalPrefix { get { return ""; } }
        public string GlobalSuffix { get { return ""; } }
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
        public string GlobalPrefix { get { return ""; } }
        public string GlobalSuffix { get { return ""; } }
    }

    abstract class IntellisenseProjectEmbedder : ICodeLanguageEmbedder
    {
        public abstract IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code);
        public abstract string ProviderName { get; }

        Guid FindGuid()
        {
            using (var settings = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration))
            using (var languages = settings.OpenSubKey("Languages"))
            using (var intellisenseProviders = languages.OpenSubKey("IntellisenseProviders"))
            using (var provider = intellisenseProviders.OpenSubKey(ProviderName))
                return new Guid(provider.GetValue("GUID").ToString());
        }

        public void OnBlockCreated(ITextBuffer editorBuffer, LanguageProjectionBuffer projectionBuffer)
        {
            EventHandler<EventArgs> h = null;
            h = delegate
            {
                // Make sure we don't set up ContainedLanguages until the buffer is ready
                WebEditor.OnIdle -= h;
                ContainedLanguageAdapter.ForBuffer(editorBuffer).AddIntellisenseProjectLanguage(projectionBuffer, FindGuid());
            };
            WebEditor.OnIdle += h;
        }

        public virtual string GlobalPrefix { get { return ""; } }
        public virtual string GlobalSuffix { get { return ""; } }
    }

    [Export(typeof(ICodeLanguageEmbedder))]
    [ContentType("CSharp")]
    class CSharpEmbedder : IntellisenseProjectEmbedder
    {
        public override string ProviderName { get { return "CSharpCodeProvider"; } }
        public override string GlobalPrefix
        {
            get
            {
                return @"using System;
                         using System.Collections.Generic;
                         using System.Data;
                         using System.Linq;
                         using System.Text;
                         using System.Threading;
                         using System.Threading.Tasks;";
            }
        }
        public override IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code)
        {
            return new[] { @"partial class Entry
                            {
                                  object SampleMethod" + Guid.NewGuid().ToString("n") + @"()
                            {",
                            @"}
                            }" };
        }
    }

    [Export(typeof(ICodeLanguageEmbedder))]
    [ContentType("Basic")]
    class VBEmbedder : IntellisenseProjectEmbedder
    {
        public override string ProviderName { get { return "VBCodeProvider"; } }
        public override string GlobalPrefix
        {
            get
            {
                return @"Imports System
                        Imports System.Collections.Generic
                        Imports System.Data
                        Imports System.Linq
                        Imports System.Text
                        Imports System.Threading
                        Imports System.Threading.Tasks";
            }
        }
        public override IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code)
        {
            return new[] { @"
                            Partial Class Entry
                            Function SampleMethod" + Guid.NewGuid().ToString("n") + @"() As Object", @"
                                Return Nothing
                            End Function
                            End Class"};
        }
    }
}
