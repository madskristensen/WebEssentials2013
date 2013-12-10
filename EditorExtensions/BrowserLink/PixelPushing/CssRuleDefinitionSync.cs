using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CSS.Core;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.BrowserLink.PixelPushing
{
    public static class CssRuleDefinitionSync
    {
        private static Dictionary<string, string> ProduceKeyValuePairsFromRuleSet(RuleSet ruleSet)
        {
            var dict = new Dictionary<string, string>();

            foreach (var declaration in ruleSet.Block.Children.OfType<Declaration>())
            {
                dict[declaration.PropertyName.Text] = declaration.StyleSheet.Text.Substring(declaration.Values.TextStart, declaration.Values.TextLength);
            }

            return dict;
        }

        private static void MoveCursorToEdit(Window window, IRange range, int offset = 0)
        {
            var selection = window.Document.Selection as TextSelection;

            if (selection != null)
            {
                try
                {
                    var buffer = ProjectHelpers.GetCurentTextBuffer();
                    int line;
                    int column;

                    buffer.GetLineColumnFromPosition(range.Start, out line, out column);
                    selection.GotoLine(line + 1 + offset);
                }
                catch
                {
                }
            }
        }

        private static CssRuleBlockSyncAction CreateDeleteAction(RuleSet rule, string item)
        {
            return (window, edit) =>
            {
                var decl = rule.Block.Children.OfType<Declaration>().LastOrDefault(x => x.PropertyName.Text == item);

                if (decl != null)
                {
                    MoveCursorToEdit(window, decl);
                    edit.Delete(decl.Start, decl.Length);

                    var pos = decl.Start;

                    while (string.IsNullOrWhiteSpace(edit.Snapshot.GetText(--pos, 1)))
                    {
                        edit.Delete(pos, 1);
                    }
                }
            };
        }

        private static CssRuleBlockSyncAction CreateUpdateAction(RuleSet rule, string propertyName, string newValue)
        {
            return (window, edit) =>
            {
                var decl = rule.Block.Children.OfType<Declaration>().LastOrDefault(x => x.PropertyName.Text == propertyName);

                if (decl != null)
                {
                    MoveCursorToEdit(window, decl);

                    edit.Delete(decl.Values.TextStart, decl.Values.TextLength);
                    edit.Insert(decl.Values.TextStart, newValue);
                }
            };
        }

        private static CssRuleBlockSyncAction CreateAddAction(RuleSet rule, string propertyName, string newValue)
        {
            return (window, edit) =>
            {
                var openingBrace = rule.Block.Children.First();

                MoveCursorToEdit(window, openingBrace, 1);

                var startPosition = openingBrace.Start + openingBrace.Length; //Just after opening brace
                var newDeclarationText = propertyName + ":" + newValue + ";";

                edit.Insert(startPosition, newDeclarationText);
            };
        }

        class SyncAction : IEquatable<SyncAction>
        {
            public RuleSet Rule { get; private set; }
            public string PropertyName { get; private set; }
            public string NewValue { get; set; }
            public CssDeltaAction ActionKind { get; set; }
            public CssRuleBlockSyncAction Action { get; set; }
            public int ApproximatePosition { get; set; }

            private SyncAction(RuleSet rule, string propertyName)
            {
                Rule = rule;
                PropertyName = propertyName;
            }

            public SyncAction ToDelete()
            {
                ActionKind = CssDeltaAction.Delete;

                var decl = Rule.Block.Children.OfType<Declaration>().FirstOrDefault(x => x.PropertyName.Text == PropertyName);

                if (decl == null)
                {
                    ApproximatePosition = -1;
                    Action = (window, edit) => { };

                    return this;
                }

                ApproximatePosition = decl.Start;

                Action = CreateDeleteAction(Rule, PropertyName);

                return this;
            }

            public SyncAction ToUpdate(string newValue = null)
            {
                var decl = Rule.Block.Children.OfType<Declaration>().FirstOrDefault(x => x.PropertyName.Text == PropertyName);

                if (decl == null)
                {
                    return ToAdd(newValue);
                }

                ApproximatePosition = decl.Start;
                ActionKind = CssDeltaAction.Update;
                NewValue = newValue ?? NewValue;
                Action = CreateUpdateAction(Rule, PropertyName, NewValue);

                return this;
            }

            public SyncAction ToAdd(string newValue = null)
            {
                ApproximatePosition = Rule.Block.Children.First().Start;
                ActionKind = CssDeltaAction.Add;
                NewValue = newValue ?? NewValue;
                Action = CreateAddAction(Rule, PropertyName, NewValue);

                return this;
            }

            public SyncAction ToNoOp()
            {
                ActionKind = CssDeltaAction.NoOp;
                Action = (window, edit) => { };

                return this;
            }

            public static SyncAction Delete(RuleSet rule, string propertyName)
            {
                return new SyncAction(rule, propertyName).ToDelete();
            }

            public static SyncAction Add(RuleSet rule, string propertyName, string value)
            {
                return new SyncAction(rule, propertyName).ToAdd(value);
            }

            public static SyncAction Update(RuleSet rule, string propertyName, string value)
            {
                return new SyncAction(rule, propertyName).ToUpdate(value);
            }

            public bool Equals(SyncAction other)
            {
                return !ReferenceEquals(other, null) && other.ActionKind == ActionKind && other.PropertyName == PropertyName;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as SyncAction);
            }

            public override int GetHashCode()
            {
                return ActionKind.GetHashCode() ^ PropertyName.GetHashCode();
            }

            public static SyncAction NoOp(RuleSet rule, string propertyName)
            {
                return new SyncAction(rule, propertyName).ToNoOp();
            }
        }

        private static bool IsValueAnyRepetitionOfInitial(string value)
        {
            const string initialPattern = "initial ";
            var trimmedValue = value.Trim() + " ";

            if (trimmedValue.Length % initialPattern.Length != 0)
            {
                return false;
            }

            for (var i = 0; i < trimmedValue.Length; i += initialPattern.Length)
            {
                for (var j = 0; j < initialPattern.Length; ++j)
                {
                    if (trimmedValue[i + j] != initialPattern[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static List<SyncAction> SimplifySyncActions(IEnumerable<SyncAction> actions)
        {
            var grouped = actions.GroupBy(x => x.PropertyName);
            var result = new List<SyncAction>();

            foreach (var group in grouped)
            {
                var contents = group.ToList();

                if (contents.Count == 1)
                {
                    result.Add(contents[0]);
                }
                else
                {
                    var distinct = contents.Distinct().ToList();
                    var lookup = distinct.ToDictionary(x => x.ActionKind);

                    if (lookup.ContainsKey(CssDeltaAction.Delete))
                    {
                        if (lookup.ContainsKey(CssDeltaAction.Add))
                        {
                            result.Add(lookup[CssDeltaAction.Add].ToUpdate());
                        }
                        else if (lookup.ContainsKey(CssDeltaAction.Update))
                        {
                            result.Add(lookup[CssDeltaAction.Update]);
                        }
                        else if (lookup.ContainsKey(CssDeltaAction.NoOp))
                        {
                        }
                        else
                        {
                            result.Add(lookup[CssDeltaAction.Delete]);
                        }
                    }
                    else
                    {
                        result.AddRange(distinct);
                    }
                }
            }

            return result;
        }

        private static IEnumerable<CssRuleBlockSyncAction> OrderAndExtractSyncActions(IEnumerable<SyncAction> actions)
        {
            return actions.Where(x => x.ActionKind != CssDeltaAction.NoOp).OrderByDescending(x => x.ApproximatePosition).Select(x => x.Action);
        }

        private static HashSet<SyncAction> CompareNewRuleAndOldRule(RuleSet comparisonRuleSet, IEnumerable<KeyValuePair<string, string>> newRule, IDictionary<string, string> oldRule)
        {
            var oldToNewDiff = new HashSet<SyncAction>();

            foreach (var entry in newRule)
            {
                string oldValue;

                if (oldRule.TryGetValue(entry.Key, out oldValue))
                {
                    //Possible Update
                    if (!string.Equals(oldValue, entry.Value))
                    {
                        if (!IsValueAnyRepetitionOfInitial(entry.Value))
                        {
                            //Update
                            oldToNewDiff.Add(SyncAction.Update(comparisonRuleSet, entry.Key, entry.Value));
                        }
                        else
                        {
                            //Delete
                            oldToNewDiff.Add(SyncAction.Delete(comparisonRuleSet, entry.Key));
                        }
                    }
                    else
                    {
                        //Same - don't do anything
                        oldToNewDiff.Add(SyncAction.NoOp(comparisonRuleSet, entry.Key));
                    }
                }
                else if (!IsValueAnyRepetitionOfInitial(entry.Value))
                {
                    //Add
                    oldToNewDiff.Add(SyncAction.Add(comparisonRuleSet, entry.Key, entry.Value));
                }
            }

            return oldToNewDiff;
        }

        private static IEnumerable<SyncAction> ReconcileExistingRuleWithDiff(RuleSet comparisonRuleSet, ICollection<SyncAction> oldToNewDiff)
        {
            var existingDict = ProduceKeyValuePairsFromRuleSet(comparisonRuleSet);
            var priorityDiff = new HashSet<SyncAction>();

            foreach (var element in oldToNewDiff.Where(x => x.ActionKind == CssDeltaAction.Update || x.ActionKind == CssDeltaAction.Delete))
            {
                var parts = element.PropertyName.Split('-');

                //If the declaration we're looking at is for a property that we've processed in the old -> new rule diff, don't bother with it, it'll get cleaned up on its own
                if (parts[0].Length == 0 || existingDict.ContainsKey(element.PropertyName))
                {
                    continue;
                }

                //Start looking for expansions on the base property that we're examining and delete the ones that aren't explicitly otherwise specified in our diff
                //Ex. If we have a record "background", locate "background-color", "background-attachment", etc and delete them if we didn't get them back from the browser (they've been shorthanded)
                var prefix = "";

                for (var i = 0; i < parts.Length - 1; ++i)
                {
                    if (prefix.Length > 0)
                    {
                        prefix += "-";
                    }

                    prefix += parts[i];

                    var localPrefix = prefix;

                    foreach (var conflictingValue in existingDict.Keys.Where(x => x.StartsWith(localPrefix, StringComparison.Ordinal)))
                    {
                        //Leave manually specified long form declarations intact but remove ones that are now shorthand
                        if (!oldToNewDiff.Contains(SyncAction.NoOp(comparisonRuleSet, conflictingValue)))
                        {
                            priorityDiff.Add(SyncAction.Delete(comparisonRuleSet, conflictingValue));
                        }
                    }
                }
            }

            return priorityDiff;
        }

        public static IEnumerable<CssRuleBlockSyncAction> ComputeSyncActions(RuleSet existingRule, string newText, string oldText = null)
        {
            var realOldText = oldText ?? existingRule.Text;
            var parser = new CssParser();
            var oldDoc = parser.Parse(realOldText, false);
            var oldDict = ProduceKeyValuePairsFromRuleSet(oldDoc.Children.OfType<RuleSet>().Single());

            if (newText == null && oldText != null)
            {
                return OrderAndExtractSyncActions(oldDict.Keys.Select(x => SyncAction.Delete(existingRule, x)));
            }

            var newDoc = parser.Parse(newText, false);
            var newDict = ProduceKeyValuePairsFromRuleSet(newDoc.Children.OfType<RuleSet>().Single());
            var oldToNewDiff = CompareNewRuleAndOldRule(existingRule, newDict, oldDict);
            var processedElements = new HashSet<string>(oldToNewDiff.Select(x => x.PropertyName));

            foreach (var item in oldDict.Keys.Except(processedElements))
            {
                //Delete
                oldToNewDiff.Add(SyncAction.Delete(existingRule, item));
                processedElements.Add(item);
            }

            if (oldText == null)
            {
                return oldToNewDiff.Select(x => x.Action);
            }

            var priorityDiff = ReconcileExistingRuleWithDiff(existingRule, oldToNewDiff);
            var combined = priorityDiff.Union(oldToNewDiff);
            var simplified = SimplifySyncActions(combined);

            return OrderAndExtractSyncActions(simplified);
        }

        public static async Task<IEnumerable<CssRuleBlockSyncAction>> ComputeSyncActionsAsync(RuleSet existingRule, string newText, string oldText = null)
        {
            return await Task.Factory.StartNew(() => ComputeSyncActions(existingRule, newText, oldText));
        }
    }
}