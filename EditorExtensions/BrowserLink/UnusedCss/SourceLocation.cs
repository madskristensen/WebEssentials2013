using System;
using Newtonsoft.Json;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class SourceLocation : IEquatable<SourceLocation>
    {
        [JsonProperty("sourcePath")]
        public string FileName { get; set; }
        [JsonProperty("startPosition")]
        public int Offset { get; set; }
        [JsonProperty("length")]
        public int Length { get; set; }

        public bool Equals(SourceLocation other)
        {
            return !ReferenceEquals(other, null) && other.FileName == FileName && other.Offset == Offset && other.Length == Length;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SourceLocation);
        }

        public override int GetHashCode()
        {
            return FileName.GetHashCode() ^ Offset ^ Length;
        }
    }
}