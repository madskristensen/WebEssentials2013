using System;
using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class RuleUsage : IEquatable<RuleUsage>
    {
        public List<string> ReferencingXPaths { get; set; }

        public CssRule Rule { get; set; }
        public bool Equals(RuleUsage other)
        {
            return !ReferenceEquals(other, null) && other.Rule.Equals(Rule) && other.ReferencingXPaths.OrderBy(x => x).SequenceEqual(ReferencingXPaths.OrderBy(x => x));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RuleUsage);
        }

        public override int GetHashCode()
        {
            return Rule.GetHashCode() ^ ReferencingXPaths.Aggregate(0, (i, c) => i ^ c.GetHashCode());
        }
    }
}
