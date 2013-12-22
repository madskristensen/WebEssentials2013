using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class RawRuleUsage : IEquatable<RawRuleUsage>
    {
        [JsonProperty]
        public string Selector { get; set; }
        [JsonProperty]
        public IEnumerable<SourceLocation> SourceLocations { get; private set; }

        public RawRuleUsage()
        {
            SourceLocations = new List<SourceLocation>();
        }

        public bool Equals(RawRuleUsage other)
        {
            return other != null && other.Selector == Selector;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RawRuleUsage);
        }

        public override int GetHashCode()
        {
            return Selector.GetHashCode();
        }
    }
}