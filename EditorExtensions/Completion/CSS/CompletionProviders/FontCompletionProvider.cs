﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CSS.Editor.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssCompletionProvider))]
    [Name("FontCompletionProvider")]
    internal class FontCompletionProvider : ICssCompletionListProvider, ICssCompletionCommitListener
    {
        public CssCompletionContextType ContextType
        {
            get { return CssCompletionContextType.PropertyValue; }
        }

        public static bool IsFontFamilyContext(CssCompletionContext context)
        {
            if (context != null && context.ContextItem != null)
            {
                Declaration decl = context.ContextItem.Parent as Declaration;
                string propertyName = (decl != null && decl.PropertyName != null) ? decl.PropertyName.Text : string.Empty;

                // Currently, only "font-family" will show font names, so just hard-code that name.
                if (propertyName == "font-family")
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<ICssCompletionListEntry> GetListEntries(CssCompletionContext context)
        {
            if (!IsFontFamilyContext(context))
                yield break;

            StyleSheet stylesheet = context.ContextItem.StyleSheet;
            var visitorRules = new CssItemCollector<FontFaceDirective>();
            stylesheet.Accept(visitorRules);

            foreach (FontFaceDirective item in visitorRules.Items)
            {
                var visitorDec = new CssItemCollector<Declaration>();
                item.Block.Accept(visitorDec);

                Declaration family = visitorDec.Items.FirstOrDefault(i => i.PropertyName.Text == "font-family");

                if (family != null)
                {
                    string value = string.Join(string.Empty, family.Values.Select(v => v.Text));
                    yield return new FontFamilyCompletionListEntry(value.Trim('\'', '"'));
                }
            }

            yield return new FontFamilyCompletionListEntry("Pick from file...");
        }

        public void OnCommitted(ICssCompletionListEntry entry, ITrackingSpan contextSpan, SnapshotPoint caret, ITextView textView)
        {
            if (entry.DisplayText == "Pick from file...")
            {
                string fontFamily;
                string atDirective = GetFontFromFile(entry.DisplayText, (IWpfTextView)textView, out fontFamily);
                if (atDirective == null)
                    return;                 // If the user cancelled the dialog, do nothing.

                Dispatcher.CurrentDispatcher.BeginInvoke(
                new Action(() => Replace(contextSpan, textView, atDirective, fontFamily)), DispatcherPriority.Normal);
            }
        }

        private static void Replace(ITrackingSpan contextSpan, ITextView textView, string atDirective, string fontFamily)
        {
            EditorExtensionsPackage.DTE.UndoContext.Open("Embed font");
            textView.TextBuffer.Insert(0, atDirective + Environment.NewLine + Environment.NewLine);
            textView.TextBuffer.Insert(contextSpan.GetSpan(textView.TextBuffer.CurrentSnapshot).Start, fontFamily);
            EditorExtensionsPackage.DTE.UndoContext.Close();
        }

        private static object _syncRoot = new object();
        private string GetFontFromFile(string text, IWpfTextView view, out string fontFamily)
        {
            lock (_syncRoot)
            {
                fontFamily = text;
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = Path.GetDirectoryName(EditorExtensionsPackage.DTE.ActiveDocument.FullName);
                dialog.Filter = "Fonts (*.woff;*.eot;*.ttf;*.otf;*.svg)|*.woff;*.eot;*.ttf;*.otf;*.svg";
                dialog.DefaultExt = ".woff";

                if (dialog.ShowDialog() != DialogResult.OK)
                    return null;

                FontDropHandler fdh = new FontDropHandler(view);
                return fdh.GetCodeFromFile(dialog.FileName, out fontFamily);
            }
        }
    }
}
