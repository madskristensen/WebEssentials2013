using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Utilities;

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
            Declaration dec = context.ContextItem.FindType<Declaration>();

            if (dec == null || dec.PropertyName == null || (!dec.PropertyName.Text.EndsWith("animation-name", StringComparison.OrdinalIgnoreCase) && dec.PropertyName.Text != "animation"))
                yield break;

            StyleSheet stylesheet = context.ContextItem.StyleSheet;
            var visitor = new CssItemCollector<KeyFramesDirective>();
            stylesheet.Accept(visitor);

            foreach (string name in visitor.Items.Select(d => d.Name.Text).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                yield return new CompletionListEntry(name);
            }
        }
    }
}
