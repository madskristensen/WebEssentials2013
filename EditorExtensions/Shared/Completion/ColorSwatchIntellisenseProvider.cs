using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Css.Extensions;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Shared
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("ColorSwatchIntellisenseProvider")]
    internal class ColorSwatchIntellisenseProvider : ICssCompletionListProvider
    {
        readonly static IEnumerable<string> _elegibleTags = new[] { "color", "background-color" };

        public CssCompletionContextType ContextType
        {
            get { return CssCompletionContextType.PropertyValue; }
        }

        public static bool IsColorContext(CssCompletionContext context)
        {
            if (context != null && context.ContextItem != null && context.ContextItem.StyleSheet is CssStyleSheet)
            {
                Declaration dec = context.ContextItem.FindType<Declaration>();
                string propertyName = (dec != null && dec.PropertyName != null) ? dec.PropertyNameText : string.Empty;

                if (_elegibleTags.Contains(propertyName))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            if (!IsColorContext(context))
                yield break;

            StyleSheet styleSheet = context.ContextItem.StyleSheet;
            CssVariableHelpers helper = ((CssStyleSheet)styleSheet).CreateVariableHelpers();
            var declarations = helper.FindDeclaredVariables(styleSheet, context.SpanStart);

            // First collect variables from current document
            var varsCollection = new CssColorVariableCollector(declarations.Select(c => new CssVariable(c.VariableName.Text, c.Value.Text)));

            // Next, collect from imported documents
            using (MultiDocumentReadLock locks = CssDocumentHelpers.DocumentImportManager.LockImportedStyleSheets(styleSheet))
            {
                foreach (StyleSheet importedStyleSheet in locks.StyleSheets)
                {
                    declarations = helper.FindDeclaredVariables(importedStyleSheet, importedStyleSheet.AfterEnd);
                    varsCollection.AddMany(declarations.Select(c => new CssVariable(c.VariableName.Text, c.Value.Text)));
                }
            }

            foreach (var variable in varsCollection.GetEvaluatedColorVariables())
            {
                yield return new ColorSwatchIntellisense(variable.Name, variable.Value);
            }
        }

        private class CssColorVariableCollector
        {
            private List<CssVariable> _variableList;
            private static readonly char[] _grammerCharacters = new[] { ' ', '(', ')' };
            private static readonly Regex _colorTokenRegex = new Regex(@"\d+|#\w+|\w+", RegexOptions.Compiled);
            private static readonly Regex _colorVariableRegex = new Regex(@"@\w+(?:-\w+)*|$\w+(?:-\w+)*", RegexOptions.Compiled);

            internal CssColorVariableCollector(IEnumerable<CssVariable> variableCollection)
            {
                this._variableList = variableCollection.ToList();
            }

            internal void AddMany(IEnumerable<CssVariable> variables)
            {
                _variableList.AddRange(variables);
            }

            internal IEnumerable<CssVariable> GetEvaluatedColorVariables()
            {
                bool hasColorValue;
                string[] values;
                string value;

                foreach (var variable in _variableList.Reverse<CssVariable>())
                {
                    hasColorValue = false;
                    values = Flatten(variable.Value, new[] { variable.Name }.ToList(), ref hasColorValue);

                    if (!hasColorValue || values == null || values.Any(v => string.IsNullOrEmpty(v)))
                        continue;

                    value = Evaluate(values);

                    if (value == null)
                        continue;

                    yield return new CssVariable(variable.Name, value);
                }
            }

            private static int? Evaluate(string expression)
            {
                try
                {
                    using (var dataTable = new DataTable())
                    {
                        dataTable.Locale = CultureInfo.CurrentCulture;

                        var dataColumn = new DataColumn("Eval", typeof(double), expression);

                        dataTable.Columns.Add(dataColumn);
                        dataTable.Rows.Add(0);

                        return (int)(double)(dataTable.Rows[0]["Eval"]);
                    }
                }
                catch
                {
                    return null;
                }
            }

            private static string Evaluate(string[] expressions)
            {
                var R = Evaluate(expressions[0]);
                var G = Evaluate(expressions[1]);
                var B = Evaluate(expressions[2]);

                if (!R.HasValue || !G.HasValue || !B.HasValue)
                    return null;

                return "#" + Math.Min(R.Value, 255).ToString("X", CultureInfo.CurrentCulture) +
                             Math.Min(G.Value, 255).ToString("X", CultureInfo.CurrentCulture) +
                             Math.Min(B.Value, 255).ToString("X", CultureInfo.CurrentCulture);
            }

            private string[] Flatten(string variableValue, List<string> alreadyProcessed, ref bool hasColorValue)
            {
                var invalid = ">>INVALID<<";

                while (_colorVariableRegex.IsMatch(variableValue))
                {
                    variableValue = _colorVariableRegex.Replace(variableValue, new MatchEvaluator(match =>
                    {
                        if (alreadyProcessed.Contains(match.Value) || !_variableList.Any(v => v.Name == match.Value))
                            return invalid;

                        return _variableList.Where(v => v.Name == match.Value).FirstOrDefault().Value;
                    }));

                    if (variableValue.Contains(invalid))
                        return null;
                }

                var parsedItems = variableValue.Split(_grammerCharacters);

                if (parsedItems.Any(i => ColorParser.TryParseColor(i, ColorParser.Options.AllowNames | ColorParser.Options.LooseParsing) != null))
                    hasColorValue = true;

                var retValue = new[]{
                    _colorTokenRegex.Replace(variableValue, new MatchEvaluator(match => GetChannelExpression(match, 'R') ?? invalid)),
                    _colorTokenRegex.Replace(variableValue, new MatchEvaluator(match => GetChannelExpression(match, 'G') ?? invalid)),
                    _colorTokenRegex.Replace(variableValue, new MatchEvaluator(match => GetChannelExpression(match, 'B') ?? invalid))
                };

                return retValue.Any(v => v.Contains(invalid)) ? null : retValue;
            }

            private static string GetChannelExpression(Match match, char channel)
            {
                var color = ColorParser.TryParseColor(match.Value, ColorParser.Options.AllowNames | ColorParser.Options.LooseParsing);
                int num = 0;

                if (color == null && !int.TryParse(match.Value, out num))
                    return null;
                else if (color != null)
                {
                    if (channel == 'R')
                        return color.Color.R.ToString(CultureInfo.CurrentCulture);

                    if (channel == 'G')
                        return color.Color.G.ToString(CultureInfo.CurrentCulture);

                    return color.Color.B.ToString(CultureInfo.CurrentCulture);
                }

                return Math.Min(num, 255).ToString(CultureInfo.CurrentCulture);
            }
        }

        private class CssVariable
        {
            internal string Name { get; private set; }
            internal string Value { get; private set; }

            internal CssVariable(string name, string value)
            {
                this.Name = name;
                this.Value = value;
            }
        }
    }
}
