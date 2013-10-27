using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
