using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions.BrowserLink.PixelPushing
{
    public class CssRuleDefinitionSync
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

        private static CssRuleBlockSyncAction CreateDeleteAction(RuleSet rule, string item)
        {
            return (window, edit) =>
            {
                var decl = rule.Block.Children.OfType<Declaration>().LastOrDefault(x => x.PropertyName.Text == item);
                if (decl != null)
                {
                    edit.Delete(decl.Start, decl.Length); var pos = decl.Start;

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
                    edit.Delete(decl.Values.TextStart, decl.Values.TextLength);
                    edit.Insert(decl.Values.TextStart, newValue);
                }
            };
        }

        private static CssRuleBlockSyncAction CreateAddAction(RuleSet rule, string propertyName, string newValue)
        {
            return (window, edit) =>
            {
                var startPosition = rule.Block.Children.Last().Start; //Just before closing brace
                var newDeclarationText = propertyName + ":" + newValue + ";";
                edit.Insert(startPosition, newDeclarationText);
            };
        }

        public class SyncAction : IEquatable<SyncAction>
        {
            public SyncAction(RuleSet rule, string propertyName)
            {
                Rule = rule;
                PropertyName = propertyName;
            }

            public RuleSet Rule { get; private set; }

            public string PropertyName { get; private set; }

            public string NewValue { get; set; }

            public CssDeltaAction ActionKind { get; set; }

            public CssRuleBlockSyncAction Action { get; set; }

            public SyncAction ToDelete()
            {
                ActionKind = CssDeltaAction.Delete;
                Action = CreateDeleteAction(Rule, PropertyName);
                return this;
            }

            public SyncAction ToUpdate(string newValue = null)
            {
                ActionKind = CssDeltaAction.Update;
                NewValue = newValue ?? NewValue;
                Action = CreateUpdateAction(Rule, PropertyName, NewValue);
                return this;
            }

            public SyncAction ToAdd(string newValue = null)
            {
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

            public static SyncAction NoOp(RuleSet rule,string propertyName)
            {
                return new SyncAction(rule,propertyName).ToNoOp();
            }
        }

        public static IEnumerable<CssRuleBlockSyncAction> ComputeSyncActions(RuleSet existingRule, string newText, string oldText = null)
        {
            var realOldText = oldText ?? existingRule.Text;
            var parser = new CssParser();
            var oldDoc = parser.Parse(realOldText, false);
            var oldDict = ProduceKeyValuePairsFromRuleSet(oldDoc.Children.OfType<RuleSet>().Single());
            var oldToNewDiff = new HashSet<SyncAction>();

            if (newText == null && oldText != null)
            {
                foreach (var key in oldDict.Keys)
                {
                    oldToNewDiff.Add(SyncAction.Delete(existingRule, key));
                }
                return oldToNewDiff.Select(x => x.Action);
            }

            var newDoc = parser.Parse(newText, false);
            var newDict = ProduceKeyValuePairsFromRuleSet(newDoc.Children.OfType<RuleSet>().Single());
            var processedElements = new HashSet<string>();

            foreach (var entry in newDict)
            {
                string oldValue;
                
                if (oldDict.TryGetValue(entry.Key, out oldValue))
                {
                    //Possible Update
                    if (!string.Equals(oldValue, entry.Value))
                    {
                        //Update
                        oldToNewDiff.Add(SyncAction.Update(existingRule, entry.Key, entry.Value));
                    }
                    else
                    {
                        //This is not the property you're looking for, move along
                        oldToNewDiff.Add(SyncAction.NoOp(existingRule, entry.Key));
                    }
                }
                else
                {
                    //Add
                    oldToNewDiff.Add(SyncAction.Add(existingRule, entry.Key, entry.Value));
                }

                processedElements.Add(entry.Key);
            }

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

            var existingDict = ProduceKeyValuePairsFromRuleSet(existingRule);
            var priorityDiff = new HashSet<SyncAction>();

            //This loop should look for:
            //  actions marked "update" that don't exist
            //  actions marked "delete" that don't exist
            foreach (var element in oldToNewDiff.Where(x => x.ActionKind == CssDeltaAction.Update || x.ActionKind == CssDeltaAction.Delete))
            {
                var parts = element.PropertyName.Split('-');
                
                if (parts[0].Length == 0 || existingDict.ContainsKey(element.PropertyName))
                {
                    continue;
                }

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
                        if (!oldToNewDiff.Contains(SyncAction.NoOp(existingRule, conflictingValue)))
                        {
                            priorityDiff.Add(SyncAction.Delete(existingRule, conflictingValue));
                        }
                    }
                }
            }

            var combined = priorityDiff.Union(oldToNewDiff);
            var grouped = combined.GroupBy(x => x.PropertyName);
            var result = new List<SyncAction>();

            foreach(var group in grouped)
            {
                var contents = group.ToList();
                if(contents.Count == 1)
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

            return result.Where(x => x.ActionKind != CssDeltaAction.NoOp).Select(x => x.Action);
        }

        public static async Task<IEnumerable<CssRuleBlockSyncAction>> ComputeSyncActionsAsync(RuleSet existingRule, string newText, string oldText = null)
        {
            return await Task.Factory.StartNew(() => ComputeSyncActions(existingRule, newText, oldText));
        }
    }
}