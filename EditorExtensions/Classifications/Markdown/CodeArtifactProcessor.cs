using System;
using System.Collections.Generic;
using System.Linq;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.Html.Core;
using Microsoft.Web.Core;

namespace MadsKristensen.EditorExtensions.Classifications.Markdown
{
    public class MarkdownCodeArtifactProcessor : IArtifactProcessor
    {
        public ArtifactCollection CreateArtifactCollection()
        {
            return new MarkdownCodeArtifactCollection(this);
        }

        public void GetArtifacts(ITextProvider text, ArtifactCollection artifactCollection)
        {
            var parser = new MarkdownParser(new TabAwareCharacterStream(text));
            CodeBlockInfo lastBlock = null;
            parser.ArtifactFound += (s, e) =>
            {
                var cla = e.Artifact as CodeLineArtifact;
                if (cla != null)
                {
                    if (lastBlock == null || lastBlock != cla.BlockInfo)
                    {
                        if (lastBlock != null)
                            artifactCollection.Add(new BlockBoundaryArtifact(lastBlock, BoundaryType.End));
                        lastBlock = cla.BlockInfo;
                        artifactCollection.Add(new BlockBoundaryArtifact(cla.BlockInfo, BoundaryType.Start));
                    }

                    // Don't add artifacts for HTML code lines.
                    if ((cla.BlockInfo.Language ?? "").StartsWith("htm", StringComparison.OrdinalIgnoreCase))
                    {
                        cla.BlockInfo.IsExtradited = true;
                        return;
                    }
                }
                // If we got a non-block artifact after a block end, add the end marker.
                else if (lastBlock != null && e.Artifact.Start >= lastBlock.OuterEnd.End)
                {
                    artifactCollection.Add(new BlockBoundaryArtifact(lastBlock, BoundaryType.End));
                    lastBlock = null;
                }
                artifactCollection.Add(e.Artifact);
            };
            parser.Parse();
            if (lastBlock != null)
                artifactCollection.Add(new BlockBoundaryArtifact(lastBlock, BoundaryType.End));
        }

        public bool IsReady { get { return true; } }

        public string LeftSeparator { get { return "`"; } }
        public string RightSeparator { get { return "`"; } }
        public string LeftCommentSeparator { get { return "<!--"; } }
        public string RightCommentSeparator { get { return "<!--"; } }
    }

    ///<summary>An Artifact that marks the start or end boundaries of a block of code.</summary>
    public class BlockBoundaryArtifact : Artifact, ICodeBlockArtifact
    {
        public BlockBoundaryArtifact(CodeBlockInfo blockInfo, BoundaryType type)
            : base(ArtifactTreatAs.Code, type == BoundaryType.Start ? blockInfo.OuterStart : blockInfo.OuterEnd, 0, 0, MarkdownClassificationTypes.MarkdownCode, true)
        {
            BlockInfo = blockInfo;
            Boundary = type;

            // Replace the BlockInfo's TextRanges with our created
            // artifacts so that they will be adjusted as the user
            // edits the text. TextRangeCollection shifts existing
            // artifacts as the user types elsewhere.
            if (type == BoundaryType.Start)
                BlockInfo.OuterStart = this;
            else
                BlockInfo.OuterEnd = this;
        }
        public CodeBlockInfo BlockInfo { get; private set; }
        public BoundaryType Boundary { get; private set; }
    }

    public enum BoundaryType { Start, End }

    public class MarkdownCodeArtifactCollection : ArtifactCollection
    {
        public MarkdownCodeArtifactCollection(IArtifactProcessor artifactProcessor) : base(artifactProcessor) { }

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
            // Forward the change to any collections of artifacts
            // that have been removed from this main collection.
            foreach (var bba in this.OfType<BlockBoundaryArtifact>())
            {
                if (bba.Boundary == BoundaryType.Start && bba.BlockInfo.IsExtradited)
                    bba.BlockInfo.CodeLines.ReflectTextChange(start, oldLength, newLength);
            }

            return base.ReflectTextChange(start, oldLength, newLength);
        }
    }
}
