using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.SyntaxCheck;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(Microsoft.Web.Editor.CssContentTypeDefinition.CssContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class DisplayInlineTextViewCreationListener : IWpfTextViewConnectionListener
    {
        private HashSet<Declaration> _cache = new HashSet<Declaration>();

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            foreach (ITextBuffer buffer in subjectBuffers)
            {
                CssEditorDocument doc = CssEditorDocument.FromTextBuffer(buffer);
                doc.Tree.ItemsChanged += (sender, e) => { ItemsChanged(buffer, e); };
                doc.Tree.TreeUpdated += Tree_TreeUpdated;
                InitializeCache(doc.Tree.StyleSheet);
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            foreach (ITextBuffer buffer in subjectBuffers)
            {
                CssEditorDocument doc = CssEditorDocument.FromTextBuffer(buffer);
                doc.Tree.TreeUpdated -= Tree_TreeUpdated;
            }
        }

        private void Tree_TreeUpdated(object sender, CssTreeUpdateEventArgs e)
        {
            InitializeCache(e.Tree.StyleSheet);
        }

        private void InitializeCache(StyleSheet stylesheet)
        {
            _cache.Clear();

            var visitor = new CssItemCollector<Declaration>(true);
            stylesheet.Accept(visitor);

            foreach (Declaration dec in visitor.Items.Where(d => d.PropertyName != null))
            {
                if (dec.PropertyName.Text == "display" && dec.Values.Any(v => v.Text == "inline"))
                    _cache.Add(dec);
            }
        }

        private void ItemsChanged(ITextBuffer buffer, CssItemsChangedEventArgs e)
        {
            foreach (ParseItem item in e.InsertedItems)
            {
                var visitor = new CssItemCollector<Declaration>(true);
                item.Accept(visitor);

                foreach (Declaration dec in visitor.Items)
                {
                    if (dec.PropertyName != null && dec.PropertyName.Text == "display" && dec.Values.Any(v => v.Text == "inline"))
                    {
                        _cache.Add(dec);

                        ParseItem rule = dec.Parent;
                        Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => Update(rule, buffer)), DispatcherPriority.Normal);
                    }
                }
            }

            foreach (ParseItem item in e.DeletedItems)
            {
                var visitor = new CssItemCollector<Declaration>(true);
                item.Accept(visitor);

                foreach (Declaration deleted in visitor.Items)
                {
                    if (_cache.Contains(deleted))
                    {
                        _cache.Remove(deleted);

                        ParseItem rule = deleted.Parent;
                        Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => Update(rule, buffer)), DispatcherPriority.Normal);
                    }
                }
            }
        }

        private static void Update(ParseItem rule, ITextBuffer buffer)
        {
            CssErrorTagger tagger = CssErrorTagger.FromTextBuffer(buffer);
            ParseItemList list = new ParseItemList() { rule };
            tagger.RecheckItems(list);
        }
    }

    [Export(typeof(ICssItemChecker))]
    [Name("DisplayInlineErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class DisplayInlineErrorTagProvider : ICssItemChecker
    {
        private static string[] invalidProperties = new[] { "margin-top", "margin-bottom", "height", "width" };

        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            RuleBlock rule = (RuleBlock)item;

            if (!rule.IsValid || context == null)
                return ItemCheckResult.Continue;

            bool isInline = rule.Declarations.Any(d => d.PropertyName != null && d.PropertyName.Text == "display" && d.Values.Any(v => v.Text == "inline"));
            if (!isInline)
                return ItemCheckResult.Continue;

            IEnumerable<Declaration> invalids = rule.Declarations.Where(d => invalidProperties.Contains(d.PropertyName.Text));

            foreach (Declaration invalid in invalids)
            {
                string error = string.Format(CultureInfo.InvariantCulture, Resources.BestPracticeInlineIncompat, invalid.PropertyName.Text);
                context.AddError(new SimpleErrorTag(invalid.PropertyName, error));
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(RuleBlock) }; }
        }
    }
}