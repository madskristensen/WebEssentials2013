using System;
using Newtonsoft.Json;

namespace MadsKristensen.EditorExtensions.BrowserLink.PixelPushing
{
    public class CssSelectorChangeData : IEquatable<CssSelectorChangeData>
    {
        [JsonProperty]
        public string Url { get; set; }
        [JsonProperty]
        public int RuleIndex { get; set; }
        [JsonProperty]
        public string NewValue { get; set; }
        [JsonProperty]
        public string OldValue { get; set; }
        [JsonProperty]
        public string Rule { get; set; }

        public bool Equals(CssSelectorChangeData other)
        {
            return !ReferenceEquals(other, null) && RuleIndex == other.RuleIndex
                   && string.Equals(Rule, other.Rule, StringComparison.Ordinal)
                   && string.Equals(Url, other.Url, StringComparison.Ordinal)
                   && string.Equals(OldValue, other.OldValue, StringComparison.Ordinal)
                   && string.Equals(NewValue, other.NewValue, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CssSelectorChangeData);
        }

        public override int GetHashCode()
        {
            return Url.GetHashCode() ^ RuleIndex ^ OldValue.GetHashCode() ^ NewValue.GetHashCode();
        }
    }
}