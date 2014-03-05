using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Css.Extensions;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;
using NCalc;

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
            private static readonly char[] _operators = new[] { '+', '-', '*', '/' };
            private static readonly char[] _variableSigns = new[] { '@', '$' };

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
                List<string> value;

                foreach (var variable in _variableList.Reverse<CssVariable>())
                {
                    hasColorValue = false;
                    value = Flatten(variable.Value, new[] { variable.Name }.ToList(), ref hasColorValue);

                    if (!hasColorValue || value.Any(v => string.IsNullOrEmpty(v)))
                        continue;

                    yield return new CssVariable(variable.Name, Evaluate(value));
                }
            }

            private static string Evaluate(List<string> expressions)
            {
                StringBuilder retString = new StringBuilder("#");

                Expression expression = new Expression(expressions[0]);
                int result = (int)double.Parse(expression.Evaluate().ToString(), CultureInfo.CurrentCulture);
                retString.Append(Math.Min(result, 255).ToString("X", CultureInfo.CurrentCulture));

                expression = new Expression(expressions[1]);
                result = (int)double.Parse(expression.Evaluate().ToString(), CultureInfo.CurrentCulture);
                retString.Append(Math.Min(result, 255).ToString("X", CultureInfo.CurrentCulture));

                expression = new Expression(expressions[2]);
                result = (int)double.Parse(expression.Evaluate().ToString(), CultureInfo.CurrentCulture);
                retString.Append(Math.Min(result, 255).ToString("X", CultureInfo.CurrentCulture));

                return retString.ToString();
            }

            private List<string> Flatten(string variableValue, List<string> variables, ref bool hasColorValue)
            {
                if (variableValue == null)
                    return null;

                var retValue = new List<StringBuilder> { new StringBuilder(), new StringBuilder(), new StringBuilder() };
                var token = new StringBuilder();

                for (int i = 0; i < variableValue.Length; i++)
                {
                    var character = variableValue[i];

                    if (i == variableValue.Length - 1 || (_operators.Contains(character) && token.Length > 0 && variableValue[i - 1] == ' '))
                    {
                        if (i == variableValue.Length - 1)
                            token.Append(character);

                        if (_variableSigns.Contains(token[0]))
                        {
                            if (variables.Contains(token.ToString()))
                                return null;

                            variables.Add(token.ToString());

                            var list = _variableList.Where(v => v.Name == token.ToString()).FirstOrDefault();

                            if (list == null)
                                return null;

                            var flattenedValue = Flatten(list.Value, variables, ref hasColorValue);

                            if (flattenedValue == null)
                                return null;

                            retValue[0].Append(flattenedValue[0]);
                            retValue[1].Append(flattenedValue[1]);
                            retValue[2].Append(flattenedValue[2]);
                        }
                        else
                        {
                            var color = ColorParser.TryParseColor(token.ToString(), ColorParser.Options.AllowNames | ColorParser.Options.LooseParsing);
                            int num = 0;

                            if (color == null && !int.TryParse(token.ToString(), out num))
                                return null;
                            else if (color != null)
                            {
                                hasColorValue = true;
                                retValue[0].Append(color.Color.R);
                                retValue[1].Append(color.Color.G);
                                retValue[2].Append(color.Color.B);
                            }
                            else
                            {
                                num = Math.Min(num, 255);

                                retValue[0].Append(num);
                                retValue[1].Append(num);
                                retValue[2].Append(num);
                            }
                        }

                        if (i < variableValue.Length - 1)
                        {
                            retValue[0].Append(character);
                            retValue[1].Append(character);
                            retValue[2].Append(character);
                        }

                        token.Clear();
                    }
                    else if (character != ' ')
                        token.Append(character);
                }

                return retValue.Select(b => b.ToString()).ToList();
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
