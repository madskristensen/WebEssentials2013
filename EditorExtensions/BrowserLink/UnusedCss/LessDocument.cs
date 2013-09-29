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

        private static string GetSelectorNameInternal(RuleSet ruleSet)
        {
            var currentSelectorName = ExtractSelectorName(ruleSet).Trim();
            var currentSet = ruleSet;
            var currentBlock = ruleSet.Parent as LessRuleBlock;

            while (currentSet != null && currentBlock != null)
            {
                currentSet = currentBlock.Parent as RuleSet;

                if (currentSet != null)
                {
                    currentSelectorName = ExtractSelectorName(currentSet).Trim() + " " + currentSelectorName;
                    currentBlock = currentSet.Parent as LessRuleBlock;
                }
            }

            var name = currentSelectorName.Replace(" &", "");
            var oldName = name;

            while (oldName != (name = name.Replace(" >", ">").Replace("> ", ">")))
            {
                oldName = name;
            }

            return oldName.Replace(">", " > ");
        }

        public override string GetSelectorName(RuleSet ruleSet)
        {
            return GetLessSelectorName(ruleSet, false);
        }

        internal static string GetLessSelectorName(RuleSet ruleSet, bool includeShellSelectors = true)
        {
            var block = ruleSet.Block as LessRuleBlock;

            if (!includeShellSelectors)
            {
                if (block == null || ruleSet.Block.Declarations.Count == 0 && ruleSet.Block.Directives.Count == 0 && block.RuleSets.Any())
                {
                    //If we got here, the element won't be included in the output but has children that might be
                    return null;
                }

                if (ruleSet.Selectors.Any(s => s.SimpleSelectors.Any(ss => ss.SubSelectors.Any(sss => sss is LessMixinDeclaration))))
                {
                    //Don't flag mixins
                    return null;
                }
            }

            return GetSelectorNameInternal(ruleSet);
        }
    }
}
