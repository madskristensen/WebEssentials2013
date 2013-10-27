using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Html.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Core;

namespace MadsKristensen.EditorExtensions
{
    public static class HtmlExtensions
    {
        public static bool CompareCurrent(this CharacterStream stream, string text, bool ignoreCase = false)
        {
            return stream.CompareTo(stream.Position, text.Length, text, ignoreCase);
        }

        ///<summary>Indicates whether a stream is current at the last valid character.</summary>
        ///<remarks>IsEndOfStream() returns true if the stream is _after_ the last character, at '\0'.</remarks>
        public static bool IsAtLastCharacter(this CharacterStream stream)
        {
            return stream.DistanceFromEnd <= 1;
        }

        public static string GetText(this IArtifact artifact, ITextSnapshot snapshot)
        {
            return snapshot.GetText(artifact.InnerRange.Start, artifact.InnerRange.Length);
        }
    }
}
