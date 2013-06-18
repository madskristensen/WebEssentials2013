using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CSS.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("AnimationNameCompletionProvider")]
    internal class AnimationNameCompletionProvider : ICssCompletionListProvider
    {
        public CssCompletionContextType ContextType
        {
            get { return CssCompletionContextType.PropertyValue; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            HashSet<ICssCompletionListEntry> entries = new HashSet<ICssCompletionListEntry>();
            Declaration dec = context.ContextItem.FindType<Declaration>();

            if (dec == null || dec.PropertyName == null || (!dec.PropertyName.Text.EndsWith("animation-name", StringComparison.OrdinalIgnoreCase) && dec.PropertyName.Text != "animation"))
                return entries;

            StyleSheet stylesheet = context.ContextItem.StyleSheet;
            var visitor = new CssItemCollector<KeyFramesDirective>();
            stylesheet.Accept(visitor);

            foreach (KeyFramesDirective keyframes in visitor.Items)
            {
                if (!entries.Any(e => e.DisplayText.Equals(keyframes.Name.Text, StringComparison.OrdinalIgnoreCase)))
                    entries.Add(new CompletionListEntry(keyframes.Name.Text));
            }

            return entries;
        }
    }
}
