//using Microsoft.CSS.Core;
//using Microsoft.CSS.Editor;
//using Microsoft.VisualStudio.Language.Intellisense;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Utilities;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.Linq;

//namespace MadsKristensen.EditorExtensions
//{
//    [Export(typeof(ICssSmartTagProvider))]
//    [Name("MissingPseudoSmartTagProvider")]
//    [Order(Before = "SortSmartTagProvider")]
//    internal class MissingPseudoSmartTagProvider : ICssSmartTagProvider
//    {
//        public Type ItemType
//        {
//            get { return typeof(Selector); }
//        }

//        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
//        {
//            RuleSet rule = item.FindType<RuleSet>();

//            if (rule == null)
//                yield break;

//            List<ParseItem> list = FindPseudos(item);

//            foreach (var pseudo in list)
//            {
//                ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaRootForBuffer(view.TextBuffer);
//                string selector = MissingVendorPseudoErrorTagProvider.GetSelectorText(rule);
//                string name = pseudo.Text.TrimStart(':');
//                string standardName = MissingVendorPseudoErrorTagProvider.StandardizeName(name);

//                IEnumerable<string> missingPseudos = MissingVendorPseudoErrorTagProvider.FindMissingPseudos(standardName, selector, schema).Where(p => p != pseudo.Text);

//                if (missingPseudos.Any())
//                {
//                    yield return new MissingPseudoSmartTagAction(itemTrackingSpan, (Selector)item, pseudo, missingPseudos);
//                }
//            }
//        }

//        private static List<ParseItem> FindPseudos(ParseItem item)
//        {
//            var visitorClass = new CssItemCollector<PseudoClassSelector>();
//            item.Accept(visitorClass);

//            var visitorElement = new CssItemCollector<PseudoElementSelector>();
//            item.Accept(visitorElement);

//            List<ParseItem> list = new List<ParseItem>();
//            list.AddRange(visitorClass.Items);
//            list.AddRange(visitorElement.Items);

//            return list;
//        }
//    }
//}
