//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using Microsoft.JSON.Core.Parser;
//using Microsoft.JSON.Editor.Completion;
//using Microsoft.JSON.Editor.Completion.Def;
//using Microsoft.VisualStudio.Language.Intellisense;
//using Microsoft.VisualStudio.Utilities;
//using Microsoft.Web.Editor;

//namespace MadsKristensen.EditorExtensions.JSON
//{
//    [Export(typeof(IJSONCompletionListProvider))]
//    [Name("ResJsonCompletionProvider")]
//    internal class ResJsonCompletionProvider : IJSONCompletionListProvider
//    {
//        public JSONCompletionContextType ContextType
//        {
//            get { return JSONCompletionContextType.PropertyName; }
//        }

//        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
//        {
//            var doc = EditorExtensionsPackage.DTE.ActiveDocument;

//            if (doc == null || !doc.FullName.EndsWith(".resjson", StringComparison.OrdinalIgnoreCase))
//                yield break;

//            JSONMember member = context.ContextItem as JSONMember;
//            JSONMember sibling = member.PreviousSibling as JSONMember;

//            while (sibling != null && sibling.Name != null)
//            {
//                if (!sibling.Name.Text.StartsWith("_"))
//                    break;

//                sibling = sibling.PreviousSibling as JSONMember;
//            }

//            if (sibling == null)
//                yield break;

//            string prop = "_" + sibling.Name.Text.Trim('"');

//            yield return new JSONCompletionEntry(prop, "\"" + prop + "\"", null,
//            GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic),
//            "iconAutomationText", true, context.Session as ICompletionSession);
//        }
//    }
//}