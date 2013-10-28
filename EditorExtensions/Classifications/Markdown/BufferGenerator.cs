using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.Html.Editor.ContainedLanguage;
using Microsoft.Html.Editor.Projection;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Composition;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    class MarkdownBufferGenerator : ArtifactBasedBufferGenerator
    {
        readonly ISet<IContentType> createdContentTypes = new HashSet<IContentType>();
        readonly IContentTypeRegistryService contentTypeRegistry;
        public MarkdownBufferGenerator(HtmlEditorTree editorTree, LanguageBlockCollection languageBlocks, IContentTypeRegistryService contentTypeRegistry)
            : base(editorTree, languageBlocks)
        {
            this.contentTypeRegistry = contentTypeRegistry;
        }

        protected override bool EnsureProjectionBuffer()
        {
            return true;   // We don't have any single ProjectionBuffer.  (also, this function should never be called)
        }

        protected override void RegenerateBuffer()
        {
            if (ProjectionBufferManager == null)
                return;

            foreach (var language in EditorTree.RootNode.Tree.ArtifactCollection.OfType<MarkdownCodeArtifact>()
                                               .GroupBy(a => a.Language))
            {
                var contentType = contentTypeRegistry.FromFriendlyName(language.Key);
                if (contentType == null) continue;  // If we can't identify the language, just use normal artifacts.

                PopulateLanguageBuffer(contentType, language);
            }
        }

        private void PopulateLanguageBuffer(IContentType contentType, IEnumerable<MarkdownCodeArtifact> artifacts)
        {
            var pBuffer = ProjectionBufferManager.GetProjectionBuffer(contentType);

            var contentTypeImportComposer = new ContentTypeImportComposer<ICodeLanguageEmbedder>(WebEditor.CompositionService);
            var embedder = contentTypeImportComposer.GetImport(contentType);

            var fullSource = new StringBuilder();
            var mappings = new List<ProjectionMapping>();

            foreach (var block in artifacts.GroupBy(a => a.BlockStart))
            {
                IReadOnlyCollection<string> surround = null;
                if (embedder != null)
                    surround = embedder.GetBlockWrapper(block.Select(a => a.GetText(EditorTree.TextSnapshot)));

                if (surround != null)
                    fullSource.AppendLine(surround.FirstOrDefault());

                foreach (var artifact in block)
                {
                    if (artifact.Start >= EditorTree.TextSnapshot.Length || artifact.End > EditorTree.TextSnapshot.Length || artifact.TreatAs != ArtifactTreatAs.Code)
                        continue;

                    mappings.Add(new ProjectionMapping(artifact.InnerRange.Start, fullSource.Length, artifact.InnerRange.Length, AdditionalContentInclusion.All));
                    fullSource.AppendLine(artifact.GetText(EditorTree.TextSnapshot));
                }

                if (surround != null)
                    fullSource.AppendLine(surround.LastOrDefault());
            }
            pBuffer.SetTextAndMappings(fullSource.ToString(), mappings.ToArray());

            if (createdContentTypes.Add(contentType))
                if (embedder != null)
                    embedder.OnBlockCreated(EditorTree.TextBuffer, pBuffer);
        }
    }

    static class ContentTypeExtensions
    {
        static readonly IReadOnlyDictionary<string, string> ContentTypeAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "C#",             "CSharp" },
            { "CS",             "CSharp" },
            { "VB",             "Basic" },
            { "VB.Net",         "Basic" },
            { "VisualBasic",    "Basic" },
            { "JS",             "Javascript" },
            { "JScript",        "Javascript" }
        };

        public static IContentType FromFriendlyName(this IContentTypeRegistryService registry, string friendlyName)
        {
            if (string.IsNullOrWhiteSpace(friendlyName))
                return null;
            if (friendlyName.Equals("html", StringComparison.OrdinalIgnoreCase) || friendlyName.Equals("htmlx", StringComparison.OrdinalIgnoreCase))
                return new NonHtmlContentType(registry.GetContentType("htmlx"));

            string realName;
            if (!ContentTypeAliases.TryGetValue(friendlyName, out realName))
                realName = friendlyName;

            var ctype = registry.GetContentType(realName);
            if (ctype == null) return null;
            if (ctype.IsOfType("htmlx"))
                return new NonHtmlContentType(ctype);
            return ctype;
        }

        ///<summary>A wrapper around the HTMLX content type that lies in IsOfType().</summary>
        ///<remarks>ProjectionBufferManager.GetProjectionBuffer() refuses to get a buffer for HTMLX, so I pass this ContentType to lie to it.</remarks>
        class NonHtmlContentType : IContentType
        {
            readonly IContentType actual;
            public NonHtmlContentType(IContentType actual) { this.actual = actual; }
            public IEnumerable<IContentType> BaseTypes { get { return actual.BaseTypes; } }
            public string DisplayName { get { return actual.DisplayName; } }
            public string TypeName { get { return actual.TypeName; } }

            public bool IsOfType(string type)
            {
                if (type == "htmlx")
                    return false;
                return actual.IsOfType(type);
            }

            public override string ToString() { return actual.ToString() + " - Fake"; }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                var fakeout = obj as NonHtmlContentType;
                if (fakeout != null)
                    return this.actual == fakeout.actual;

                return actual.Equals(obj);
            }
            public override int GetHashCode()
            {
                return actual.GetHashCode();
            }
        }
    }
}
