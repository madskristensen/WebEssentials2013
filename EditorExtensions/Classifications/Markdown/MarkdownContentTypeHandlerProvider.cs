using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Html.Editor;
using Microsoft.Html.Editor.ContainedLanguage;
using System.Reflection;
using Microsoft.Html.Core;
using Microsoft.Web.Core;
using Microsoft.Web.Editor;
using Microsoft.Html.Editor.Projection;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    [Export(typeof(IContentTypeHandlerProvider))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    public class MarkdownContentTypeHandlerProvider : IContentTypeHandlerProvider
    {
        [Import]
        public IContentTypeRegistryService ContentTypeRegistry { get; set; }

        public IContentTypeHandler GetContentTypeHandler()
        {
            return new MarkdownContentTypeHandler(ContentTypeRegistry);
        }
    }

    public class MarkdownContentTypeHandler : HtmlContentTypeHandler
    {
        static readonly Func<HtmlContentTypeHandler, List<LanguageBlockHandler>> GetLanguageBlockHandlerList =
            (Func<HtmlContentTypeHandler, List<LanguageBlockHandler>>)
            Delegate.CreateDelegate(
                typeof(Func<HtmlContentTypeHandler, List<LanguageBlockHandler>>),
                typeof(HtmlContentTypeHandler).GetProperty("LanguageBlockHandlers", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod
            );

        readonly IContentTypeRegistryService contentTypeRegistry;
        public MarkdownContentTypeHandler(IContentTypeRegistryService contentTypeRegistry)
        {
            this.contentTypeRegistry = contentTypeRegistry;
        }

        protected override void CreateBlockHandlers()
        {
            base.CreateBlockHandlers();
            GetLanguageBlockHandlerList(this).Add(new CodeBlockBlockHandler(EditorTree, contentTypeRegistry));
        }

        public override void Init(HtmlEditorTree editorTree)
        {
            base.Init(editorTree);
            ContainedLanguageSettings.FormatOnPaste = false;
            ContainedLanguageSettings.EnableSyntaxCheck = false;
        }

        public override IContentType GetContentTypeOfLocation(int position)
        {
            int itemContaining = EditorTree.ArtifactCollection.GetItemContaining(position);
            if (itemContaining >= 0)
            {
                IArtifact artifact = EditorTree.ArtifactCollection[itemContaining];
                if (artifact.TreatAs == ArtifactTreatAs.Comment)
                {
                    return contentTypeRegistry.GetContentType("text");
                }
            }
            return base.GetContentTypeOfLocation(position);
        }
        public override ArtifactCollection CreateArtifactCollection()
        {
            return new MarkdownCodeArtifactCollection(new MarkdownCodeArtifactProcessor());
        }
    }

    class CodeBlockBlockHandler : ArtifactBasedBlockHandler
    {
        readonly IContentTypeRegistryService contentTypeRegistry;
        public CodeBlockBlockHandler(HtmlEditorTree tree, IContentTypeRegistryService contentTypeRegistry)
            : base(tree, contentTypeRegistry.GetContentType("htmlx"))
        {
            this.contentTypeRegistry = contentTypeRegistry;
        }
        protected override BufferGenerator CreateBufferGenerator()
        {
            return new MarkdownBufferGenerator(EditorTree, LanguageBlocks, contentTypeRegistry);
        }
        public override IContentType GetContentTypeOfLocation(int position)
        {
            LanguageBlock block = this.GetLanguageBlockOfLocation(position);
            if (block == null) return null;
            var alb = block as ArtifactLanguageBlock;
            if (alb == null)
                return contentTypeRegistry.GetContentType("text");
            if (string.IsNullOrWhiteSpace(alb.Language))
                return contentTypeRegistry.GetContentType("code");

            return contentTypeRegistry.GetContentType(alb.Language);
        }

        protected override void BuildLanguageBlockCollection()
        {
            ArtifactCollection artifactCollection = base.EditorTree.RootNode.Tree.ArtifactCollection;
            base.LanguageBlocks.Clear();
            foreach (MarkdownCodeArtifact current in artifactCollection)
            {
                if (current.TreatAs == ArtifactTreatAs.Code)
                {
                    base.LanguageBlocks.AddBlock(new ArtifactLanguageBlock(current));
                }
            }
            base.LanguageBlocks.SortByPosition();
        }

    }

    class ArtifactLanguageBlock : LanguageBlock
    {
        public ArtifactLanguageBlock(MarkdownCodeArtifact a) : base(a) { Language = a.Language; }
        public string Language { get; private set; }
    }

    class MarkdownBufferGenerator : ArtifactBasedBufferGenerator
    {
        readonly IContentTypeRegistryService contentTypeRegistry;
        public MarkdownBufferGenerator(HtmlEditorTree editorTree, LanguageBlockCollection languageBlocks, IContentTypeRegistryService contentTypeRegistry)
            : base(editorTree, languageBlocks)
        {
            this.contentTypeRegistry = contentTypeRegistry;
        }


        protected override void RegenerateBuffer()
        {
            if (!this.EnsureProjectionBuffer())
            {
                return;
            }
            base.RegenerateBuffer();

            foreach (var g in EditorTree.RootNode.Tree.ArtifactCollection.OfType<MarkdownCodeArtifact>()
                                        .GroupBy(a => a.Language))
            {
                var contentType = contentTypeRegistry.GetContentType(String.IsNullOrWhiteSpace(g.Key) ? "code" : g.Key);
                var pBuffer = ProjectionBufferManager.GetProjectionBuffer(contentType);

                StringBuilder fullSource = new StringBuilder();

                List<ProjectionMapping> list = new List<ProjectionMapping>();

                ITextSnapshot textSnapshot = base.EditorTree.TextSnapshot;

                this.AppendHeader(fullSource);
                foreach (var artifact in g)
                {

                    if (artifact.Start >= textSnapshot.Length || artifact.End > textSnapshot.Length || artifact.TreatAs != ArtifactTreatAs.Code)
                        continue;

                    fullSource.Append(this.BeginExternalSource);
                    int artifactStart = fullSource.Length;

                    ITextRange innerRange = artifact.InnerRange;
                    fullSource.Append(textSnapshot.GetText(innerRange.Start, innerRange.Length));
                    fullSource.Append(this.EndExternalSource);

                    ProjectionMapping item = new ProjectionMapping(innerRange.Start, artifactStart, innerRange.Length, AdditionalContentInclusion.All);
                    list.Add(item);
                }

                this.AppendFooter(fullSource);
                pBuffer.SetTextAndMappings(fullSource.ToString(), list.ToArray());
            }
        }

    }


    public class MarkdownCodeArtifactProcessor : IArtifactProcessor
    {
        public ArtifactCollection CreateArtifactCollection()
        {
            return new MarkdownCodeArtifactCollection(this);
        }

        public void GetArtifacts(ITextProvider text, ArtifactCollection artifactCollection)
        {
            var parser = new MarkdownParser(new CharacterStream(text));
            parser.ArtifactFound += (s, e) => artifactCollection.Add(e.Artifact);
            parser.Parse();
        }

        public bool IsReady { get { return true; } }

        public string LeftSeparator { get { return "`"; } }
        public string RightSeparator { get { return "`"; } }
        public string LeftCommentSeparator { get { return "<!--"; } }
        public string RightCommentSeparator { get { return "<!--"; } }
    }

    public class MarkdownCodeArtifactCollection : ArtifactCollection
    {
        public MarkdownCodeArtifactCollection(IArtifactProcessor p) : base(p) { }

        ///<summary>Checks the old and new text to see whether either text matches a condition.</summary>
        ///<param name="surroundingLength">The number of characters around the modified portion (in each direction) to include in the check.  Pass zero to only check the modified range.</param>
        ///<param name="predicate">The check to run against each version.</param>
        ///<returns>True if either version matched.</returns>
        ///<remarks>
        /// This will not check empty versions; if text is deleted 
        /// or inserted, it will only run on the wider version. It
        /// will only check both versions if the range of text was 
        /// replaced with different text (if both lengths are >0).  
        ///</remarks>
        private delegate bool CheckTextDelegate(int surroundingLength, Func<string, bool> predicate);
        private static bool CheckRange(int surroundingLength, Func<string, bool> predicate, int rangeStart, int rangeLength, ITextProvider rangeText)
        {
            if (rangeLength == 0)
                return false;
            var surroundingText = rangeText.GetText(new TextRange(
                rangeStart,
                Math.Min(rangeText.Length - rangeStart, rangeLength + surroundingLength)
            ));
            return predicate(surroundingText);
        }


        public override bool IsDestructiveChange(int start, int oldLength, int newLength, ITextProvider oldText, ITextProvider newText)
        {
            if (base.IsDestructiveChange(start, oldLength, newLength, oldText, newText))
                return true;

            CheckTextDelegate CheckText = (surroundingLength, predicate) =>
            {
                int rangeStart = Math.Max(0, start - surroundingLength);
                return CheckRange(surroundingLength, predicate, rangeStart, newLength, newText)
                    || CheckRange(surroundingLength, predicate, rangeStart, oldLength, oldText);
            };


            // If the user typed characters involved in code blocks, rebuild.
            if (CheckText(0, delta => delta.IndexOfAny(new[] { '`', '~', '\r', '\n', '\t' }) >= 0))
                return true;

            if (CheckText(0, delta => delta.Contains(' ')))
            {
                // If the user typed a space, and it looks like we're in indent for an indented code block, rebuild.
                if (CheckText(4, s => s.Contains('\t') || s.Contains("    ")))
                    return true;
            }

            return false;
        }


        public override ICollection<IArtifact> ReflectTextChange(int start, int oldLength, int newLength)
        {
            return base.ReflectTextChange(start, oldLength, newLength);
        }
    }
}
