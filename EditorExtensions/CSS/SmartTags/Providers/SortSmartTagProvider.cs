//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using Microsoft.CSS.Core;
//using Microsoft.VisualStudio.Language.Intellisense;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Utilities;

//namespace MadsKristensen.EditorExtensions.Css
//{
//    [Export(typeof(ICssSmartTagProvider))]
//    [Name("SortSmartTagProvider")]
//    internal class SortSmartTagProvider : ICssSmartTagProvider
//    {
//        public Type ItemType
//        {
//            get { return typeof(Selector); }
//        }

//        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
//        {
//            RuleSet rule = item.FindType<RuleSet>();
//            if (rule == null || !rule.IsValid || !rule.Block.IsValid)
//                yield break;

//            yield return new SortSmartTagAction(rule, itemTrackingSpan, view);
//        }
//    }
//}
