using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                                               .GroupBy(a => contentTypeRegistry.FromFriendlyName(a.Language)))
            {
                if (language.Key == null) continue;  // If we can't identify the language, just use normal artifacts.
                // We can't nest HTML buffers
                if (language.Key.IsOfType("html") || language.Key.IsOfType("htmlx"))
                    continue;
                PopulateLanguageBuffer(language.Key, language);
            }
        }

        private void PopulateLanguageBuffer(IContentType contentType, IEnumerable<MarkdownCodeArtifact> artifacts)
        {
            var pBuffer = ProjectionBufferManager.GetProjectionBuffer(contentType);

            var contentTypeImportComposer = new ContentTypeImportComposer<ICodeLanguageEmbedder>(WebEditor.CompositionService);
            var embedder = contentTypeImportComposer.GetImport(contentType);

            var fullSource = new StringBuilder();
            if (embedder != null)
                fullSource.AppendLine(embedder.GlobalPrefix);
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
            if (embedder != null)
                fullSource.AppendLine(embedder.GlobalSuffix);
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

            string realName;
            if (!ContentTypeAliases.TryGetValue(friendlyName, out realName))
                realName = friendlyName;

            return registry.GetContentType(realName);
        }
    }
}
