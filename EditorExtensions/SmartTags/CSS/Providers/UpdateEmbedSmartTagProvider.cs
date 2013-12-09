﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSmartTagProvider))]
    [Name("UpdateEmbedSmartTagProvider")]
    internal class UpdateEmbedSmartTagProvider : ICssSmartTagProvider
    {
        public Type ItemType
        {
            get { return typeof(UrlItem); }
        }

        public IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int position, ITrackingSpan itemTrackingSpan, ITextView view)
        {
            UrlItem url = (UrlItem)item;
            if (!url.IsValid || url.UrlString == null || !url.UrlString.Text.Contains(";base64,"))
                yield break;

            CComment comment = url.NextSibling as CComment;

            if (comment != null && comment.CommentText != null)
            {
                string path = comment.CommentText.Text.Trim();
                yield return new UpdateEmbedSmartTagAction(itemTrackingSpan, path);
            }
            else
            {
                RuleBlock rule = item.FindType<RuleBlock>();
                Declaration dec = item.FindType<Declaration>();

                if (rule == null || dec == null || dec.PropertyName == null)
                    yield break;

                foreach (Declaration sibling in rule.Declarations.Where(d => d.PropertyName != null && d != dec))
                {
                    if (sibling.PropertyName.Text == "*" + dec.PropertyName.Text || sibling.PropertyName.Text == "_" + dec.PropertyName.Text)
                    {
                        var visitor = new CssItemCollector<UrlItem>();
                        sibling.Accept(visitor);

                        UrlItem siblingUrl = visitor.Items.FirstOrDefault();
                        if (siblingUrl != null && siblingUrl.UrlString != null)
                        {
                            yield return new UpdateEmbedSmartTagAction(itemTrackingSpan, siblingUrl.UrlString.Text);
                            break;
                        }
                    }
                }
            }
        }
    }
}