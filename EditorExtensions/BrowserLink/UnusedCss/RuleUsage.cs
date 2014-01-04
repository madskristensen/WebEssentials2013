using System;
using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class RuleUsage : IEquatable<RuleUsage>
    {
        public HashSet<SourceLocation> SourceLocations { get; private set; }
        public IStylingRule Rule { get; set; }

        public RuleUsage()
        {
            SourceLocations = new HashSet<SourceLocation>();
        }

        public bool Equals(RuleUsage other)
        {
            return !ReferenceEquals(other, null) && other.Rule.Equals(Rule) && other.SourceLocations.SetEquals(SourceLocations);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RuleUsage);
        }

        public override int GetHashCode()
        {
            return Rule.GetHashCode() ^ SourceLocations.Aggregate(0, (i, c) => i ^ c.GetHashCode());
        }
    }
}
