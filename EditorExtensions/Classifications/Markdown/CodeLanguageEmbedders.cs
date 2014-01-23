using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Html.Editor;
using Microsoft.Html.Editor.Projection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    ///<summary>Preprocesses embedded code Markdown blocks before creating projection buffers.</summary>
    ///<remarks>
    /// Implement this interface to initialize language services for
    /// your language, or to add custom wrapping text around blocks.
    /// Implementations should be state-less; only one instance will
    /// be created.
    ///</remarks>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Embedder")]
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
    public class CssEmbedder : ICodeLanguageEmbedder
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
    public class JavaScriptEmbedder : ICodeLanguageEmbedder
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

    public abstract class IntellisenseProjectEmbedder : ICodeLanguageEmbedder
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
                // When loading lots of Markdown files on solution load, we might need to
                // wait for multiple idle cycles.
                var doc = ServiceManager.GetService<HtmlEditorDocument>(editorBuffer);
                if (doc == null) return;
                if (doc.PrimaryView == null) return;

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
    public class CSharpEmbedder : IntellisenseProjectEmbedder
    {
        public override string ProviderName { get { return "CSharpCodeProvider"; } }
        public override string GlobalPrefix
        {
            get
            {
                return @"using System;
                         using System.Collections.Generic;
                         using System.Data;
                         using System.IO;
                         using System.Linq;
                         using System.Net;
                         using System.Net.Http;
                         using System.Net.Http.Formatting;
                         using System.Reflection;
                         using System.Text;
                         using System.Threading;
                         using System.Threading.Tasks;
                         using System.Xml;
                         using System.Xml.Linq;";
            }
        }
        public override IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code)
        {
            return new[] { @"partial class Entry
                            {
                                  async Task<object> SampleMethod" + Guid.NewGuid().ToString("n") + @"() {", @"
                                return await Task.FromResult(new object());
                            }
                            }" };
        }
    }

    [Export(typeof(ICodeLanguageEmbedder))]
    [ContentType("Basic")]
    public class VBEmbedder : IntellisenseProjectEmbedder
    {
        public override string ProviderName { get { return "VBCodeProvider"; } }
        public override string GlobalPrefix
        {
            get
            {
                return @"Imports System
                        Imports System.Collections.Generic
                        Imports System.Data
                        Imports System.IO
                        Imports System.Linq
                        Imports System.Net
                        Imports System.Net.Http
                        Imports System.Net.Http.Formatting
                        Imports System.Reflection
                        Imports System.Text
                        Imports System.Threading
                        Imports System.Threading.Tasks
                        Imports System.Xml
                        Imports System.Xml.Linq";
            }
        }
        public override IReadOnlyCollection<string> GetBlockWrapper(IEnumerable<string> code)
        {
            return new[] { @"
                            Partial Class Entry
                            Async Function SampleMethod" + Guid.NewGuid().ToString("n") + @"() As Task(Of Object)", @"
                                Return Await Task.FromResult(New Object())
                            End Function
                            End Class" };
        }
    }
}