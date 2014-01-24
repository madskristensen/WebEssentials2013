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

            foreach (var language in EditorTree.RootNode.Tree.ArtifactCollection
                                               .OfType<BlockBoundaryArtifact>()
                                               .Select(b => b.BlockInfo)
                                               .Distinct()
                                               .GroupBy(b => contentTypeRegistry.FromFriendlyName(b.Language).ToEmbeddableContentType()))
            {
                if (language.Key == null) continue;  // If we can't identify the language, just use normal artifacts.
                PopulateLanguageBuffer(language.Key, language.SelectMany(b => b.CodeLines));
            }
        }

        private void PopulateLanguageBuffer(IContentType contentType, IEnumerable<CodeLineArtifact> artifacts)
        {
            var pBuffer = ProjectionBufferManager.GetProjectionBuffer(contentType);

            var embedder = Mef.GetImport<ICodeLanguageEmbedder>(contentType);

            var fullSource = new StringBuilder();
            if (embedder != null)
                fullSource.AppendLine(embedder.GlobalPrefix);
            var mappings = new List<ProjectionMapping>();

            foreach (var block in artifacts.GroupBy(a => a.BlockInfo))
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
            { "VB.Net",         "Basic" },
            { "VisualBasic",    "Basic" },
            { "JScript",        "Javascript" }
        };

        ///<summary>Finds the ContentType corresponding to a user-facing string.</summary>
        public static IContentType FromFriendlyName(this IContentTypeRegistryService registry, string friendlyName)
        {
            if (string.IsNullOrWhiteSpace(friendlyName))
                return null;

            string realName;
            if (!ContentTypeAliases.TryGetValue(friendlyName, out realName))
                realName = friendlyName;

            return registry.GetContentType(realName)
                ?? WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>().Value.GetContentTypeForExtension(friendlyName);
        }
        ///<summary>Converts a ContenType to a ContentType that can be embedded.  This function contains workarounds for issues with specific ContentTypes.</summary>
        public static IContentType ToEmbeddableContentType(this IContentType original)
        {
            if (original == null)
                return null;

            // Having both CSS LESS buffers in the same TextView
            // breaks IntelliSense.  Also, embedding CSS as LESS
            // allows CSS code blocks to have both selectors and
            // and properties together.
            if (original.IsOfType("CSS"))
                return WebEditor.ExportProvider.GetExport<IContentTypeRegistryService>().Value.GetContentType("LESS");

            // Having two HTMLX buffers within the same TextView
            // breaks most of their code.
            // The original HTML classifier doesn't work without
            // an IVsTextBuffer for the buffer.
            // Instead, we report this as normal text.
            if (original.IsOfType("htmlx") || original.IsOfType("html"))
                return null;

            return original;
        }
    }
}
