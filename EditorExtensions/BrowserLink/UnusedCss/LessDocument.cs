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
                return ruleSet.Selectors.Select(CssExtensions.SelectorText);

            var parentSet = parentBlock.Parent as RuleSet;

            if (parentSet == null)
                return ruleSet.Selectors.Select(CssExtensions.SelectorText);

            // Cache the computed parents to avoid re-computing them
            // for every child permutation.
            var parentSelectors = GetSelectorNames(parentSet).ToList();
            return ruleSet.Selectors.SelectMany(child =>
                CombineSelectors(parentSelectors, child.SelectorText())
            );
        }

        private static IEnumerable<string> CombineSelectors(ICollection<string> parents, string child)
        {
            if (!child.Contains("&"))
                return parents.Select(p => p + " " + child);

            // Build a chained LINQ query that expands every ampersand
            // into each parent to get every permutation of selectors.
            var result = Enumerable.Repeat("", 1);

            int lastIndex = 0;
            while (true)
            {
                int nextIndex = child.IndexOf('&', lastIndex);
                if (nextIndex < 0)
                {
                    // If the child selector does not end in an ampersand, append the last chunk directly
                    var chunk = child.Substring(lastIndex);
                    return result.Select(c => c + chunk);
                }
                else
                {
                    // If we got up to an ampersand, append the chunk followed by every parent selector.
                    var chunk = child.Substring(lastIndex, nextIndex - lastIndex);
                    result = result.SelectMany(c => parents.Select(p => c + chunk + p));
                }

                nextIndex++;    // Skip the ampersand
                if (nextIndex == child.Length)
                    break;
                else
                    lastIndex = nextIndex;
            }
            return result;
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
