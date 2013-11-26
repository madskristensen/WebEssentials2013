using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("DuplicatePropertyErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class DuplicatePropertyErrorTagProvider : ICssItemChecker
    {
        // The rules of this error is specified here: https://github.com/stubbornella/csslint/wiki/Disallow-duplicate-properties
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            RuleBlock rule = (RuleBlock)item;

            if (!rule.IsValid || context == null)
                return ItemCheckResult.Continue;

            Dictionary<string, string> dic = new Dictionary<string, string>();

            foreach (Declaration declaration in rule.Declarations)
            {
                ParseItem prop = declaration.PropertyName;
                if (prop == null || prop.Text == "filter")
                    continue;

                string error = null;

                if (!dic.ContainsKey(declaration.Text))
                {
                    if (dic.ContainsValue(prop.Text) && dic.Last().Value != prop.Text)
                    {
                        // The same property name is specified, but not by the immidiate previous declaration
                        error = string.Format(CultureInfo.InvariantCulture, Resources.BestPracticeDuplicatePropertyInRule, prop.Text);
                    }

                    dic.Add(declaration.Text, prop.Text);
                }
                else
                {
                    // The same property and value exist more than once in the rule. The exact declaration duplicate
                    error = string.Format(CultureInfo.InvariantCulture, Resources.BestPracticeDuplicatePropertyWithSameValueInRule, prop.Text);
                }

                if (error != null)
                {
                    context.AddError(new SimpleErrorTag(prop, error));
                }
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(RuleBlock) }; }
        }
    }
}