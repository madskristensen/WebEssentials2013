//using Microsoft.CSS.Core;
//using Microsoft.VisualStudio.Utilities;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.Linq;

//namespace MadsKristensen.EditorExtensions
//{
//    [Export(typeof(ICssItemChecker))]
//    [Name("OverriddenPropertyErrorTagProvider")]
//    [Order(After = "Default Declaration")]
//    internal class OverriddenPropertyErrorTagProvider : ICssItemChecker
//    {
//        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
//        {
//            Declaration dec = (Declaration)item;
//            RuleBlock rule = item.Parent as RuleBlock;

//            if (!dec.IsValid || context == null || rule == null)
//                return ItemCheckResult.Continue;

//            List<string> list = new List<string>();

//            foreach (Declaration declaration in rule.Declarations.Where(d => d.PropertyName != null))
//            {
//                if (declaration == dec) // Don't look beyond current declaration in the RuleBlock
//                    break;

//                ParseItem prop = declaration.PropertyName;
//                if (prop == null || prop.Text == "border-radius" || dec.PropertyName.Text == "background" || dec.Values.Any(v => v.Text.StartsWith("-")))
//                    continue;

//                if (prop.Length > dec.PropertyName.Length && prop.Text.StartsWith(dec.PropertyName.Text))
//                {
//                    list.Add(prop.Text);
//                }
//            }

//            if (list.Count > 0)
//            {
//                string message = "Best practice: '{0}' is overriding previously declared properties in the same rule block ({1})";
//                string error = string.Format(message, dec.PropertyName.Text, string.Join(", ", list.ToArray()));
//                context.AddError(new SimpleErrorTag(dec.PropertyName, error));
//            }

//            return ItemCheckResult.Continue;
//        }

//        public IEnumerable<Type> ItemTypes
//        {
//            get { return new[] { typeof(Declaration) }; }
//        }
//    }
//}