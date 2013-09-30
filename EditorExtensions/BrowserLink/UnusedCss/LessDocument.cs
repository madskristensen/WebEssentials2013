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
            return GetAllRuleSets(ruleSets);
        }
        public static IEnumerable<RuleSet> GetAllRuleSets(IEnumerable<RuleSet> ruleSets)
        {
            var result = new HashSet<RuleSet>();

            foreach (var set in ruleSets)
            {
                result.Add(set);
                var block = set.Children.OfType<LessRuleBlock>().SingleOrDefault();

                if (block != null)
                {
                    result.UnionWith(GetAllRuleSets(block.RuleSets));
                }
            }

            return result;
        }

        public static IEnumerable<string> GetSelectorNames(RuleSet ruleSet)
        {
            var parentBlock = ruleSet.Parent as LessRuleBlock;

            if (parentBlock == null)
                return Enumerable.Repeat(ExtractSelectorName(ruleSet).Trim(), 1);

            var parentSet = parentBlock.Parent as RuleSet;

            if (parentSet == null)
                return Enumerable.Repeat(ExtractSelectorName(ruleSet).Trim(), 1);

            return from parentSelector in GetSelectorNames(parentSet)
                   from childSelector in ruleSet.Selectors
                   select CombineSelectors(parentSelector, childSelector.Text);
        }

        private static string CombineSelectors(string parent, string child)
        {
            if (!child.Contains("&"))
                return parent + " " + child;
            else
                return child.Replace("&", parent);
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

            string name = string.Join("\r\n,", GetSelectorNames(ruleSet));

            var oldName = name;

            while (oldName != (name = name.Replace(" >", ">").Replace("> ", ">")))
            {
                oldName = name;
            }

            return oldName.Replace(">", " > ");
        }
    }
}
