using Microsoft.CSS.Core;
using Microsoft.Less.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class LessDocument : DocumentBase
    {
        private LessDocument(string file, FileSystemEventHandler fileDeletedCallback)
            : base(file, fileDeletedCallback)
        {
        }

        protected override ICssParser GetParser()
        {
            return new LessParser();
        }

        internal static IDocument For(string fullPath, FileSystemEventHandler fileDeletedCallback = null)
        {
            return For(fullPath, fileDeletedCallback, (f, c) => new LessDocument(f, c));
        }

        protected override IEnumerable<RuleSet> ExpandRuleSets(IEnumerable<RuleSet> ruleSets)
        {
            var result = new HashSet<RuleSet>();

            foreach (var set in ruleSets)
            {
                result.Add(set);
                var block = set.Children.OfType<LessRuleBlock>().SingleOrDefault();

                if (block != null)
                {
                    result.UnionWith(ExpandRuleSets(block.RuleSets));
                }
            }

            return result;
        }

        public override string GetSelectorName(RuleSet ruleSet)
        {
            var currentSelectorName = base.GetSelectorName(ruleSet);
            var currentSet = ruleSet;
            var currentBlock = ruleSet.Parent as LessRuleBlock;

            while (currentSet != null && currentBlock != null)
            {
                currentSet = currentBlock.Parent as RuleSet;

                if (currentSet != null)
                {
                    currentSelectorName = base.GetSelectorName(currentSet) + " " + currentSelectorName;
                    currentBlock = currentSet.Parent as LessRuleBlock;
                }
            }

            return currentSelectorName.Replace(" &:", ":");
        }
    }
}
