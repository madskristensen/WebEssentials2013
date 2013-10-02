﻿using Microsoft.CSS.Core;
using Microsoft.Less.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class LessDocument : DocumentBase
    {
        private LessDocument(string file)
            : base(file)
        {
        }

        protected override ICssParser GetParser()
        {
            return new LessParser();
        }

        internal static IDocument For(string fullPath, bool createIfRequired)
        {
            return For(fullPath, createIfRequired, f => new LessDocument(f));
        }


        public static IEnumerable<string> GetSelectorNames(RuleSet ruleSet, LessMixinAction mixinAction)
        {
            if (ruleSet.Selectors.Any(s => s.SimpleSelectors.Any(ss => ss.SubSelectors.Any(sss => sss is LessMixinDeclaration))))
            {
                switch (mixinAction)
                {
                    case LessMixinAction.Skip:
                        return Enumerable.Empty<string>();
                    case LessMixinAction.Literal:
                        break;
                    case LessMixinAction.NestedOnly:
                        var mixinDecl = ruleSet.Selectors.SelectMany(s => s.SimpleSelectors.SelectMany(ss => ss.SubSelectors.OfType<LessMixinDeclaration>())).First();
                        return Enumerable.Repeat("«mixin " + mixinDecl.MixinName.Name + "»", 1);
                }
            }

            var parentBlock = ruleSet.FindType<LessRuleBlock>();

            if (parentBlock == null)
                return ruleSet.Selectors.Select(CssExtensions.SelectorText);

            var parentSet = parentBlock.Parent as RuleSet;

            if (parentSet == null)
                return ruleSet.Selectors.Select(CssExtensions.SelectorText);

            // Cache the computed parents to avoid re-computing them
            // for every child permutation.
            var parentSelectors = GetSelectorNames(parentSet, mixinAction).ToList();
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
            if (!includeShellSelectors)
            {
                var block = ruleSet.Block as LessRuleBlock;
                if (block == null || ruleSet.Block.Declarations.Count == 0 && ruleSet.Block.Directives.Count == 0 && block.RuleSets.Any())
                {
                    //If we got here, the element won't be included in the output but has children that might be
                    return null;
                }
            }

            string name = string.Join(",\r\n", GetSelectorNames(ruleSet, includeShellSelectors ? LessMixinAction.NestedOnly : LessMixinAction.Skip));
            if (name.Length == 0)
                return null;

            var oldName = name;

            while (oldName != (name = name.Replace(" >", ">").Replace("> ", ">")))
            {
                oldName = name;
            }

            return oldName.Replace(">", " > ");
        }
    }
    ///<summary>Specifies how to handle mixins (and selectors within mixins) when constructing generated CSS selectors from LESS rulesets.</summary>
    public enum LessMixinAction
    {
        ///<summary>Return null for any selector in a mixin.</summary>
        Skip,
        ///<summary>Return the literal text of the mixin declaration.  (this is not very useful)</summary>
        Literal,
        ///<summary>Return the text of any selectors nested within a mixin (until the mixin itself), and return null for the mixin itself.</summary>
        NestedOnly
    }
}
