﻿using System;
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
    [Name("IdCompletionProvider")]
    internal class IdCompletionProvider : ICssCompletionListProvider
    {
        public CssCompletionContextType ContextType
        {
            get { return (CssCompletionContextType)603; }
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            StyleSheet stylesheet = context.ContextItem.StyleSheet;
            var visitorRules = new CssItemCollector<IdSelector>();
            stylesheet.Accept(visitorRules);

            foreach (string item in visitorRules.Items.Where(s => s != context.ContextItem).Select(s => s.Text).Distinct())
            {
                yield return new CompletionListEntry(item);
            }
        }
    }
}
