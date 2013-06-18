using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(Microsoft.Web.Editor.CssContentTypeDefinition.CssContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class MoveRuleTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            textView.Properties.GetOrCreateSingletonProperty<MoveRuleTarget>(() => new MoveRuleTarget(textViewAdapter, textView));
        }
    }

    class MoveRuleTarget : IOleCommandTarget
    {
        private ITextView _textView;
        private IOleCommandTarget _nextCommandTarget;
        private CssTree _tree;

        public MoveRuleTarget(IVsTextView adapter, ITextView textView)
        {
            this._textView = textView;
            adapter.AddCommandFilter(this, out _nextCommandTarget);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                switch (nCmdID)
                {
                    case 2400:
                        if (Move(Direction.Down))
                            return VSConstants.S_OK;
                        break;

                    case 2401:
                        if (Move(Direction.Up))
                            return VSConstants.S_OK;
                        break;
                }
            }

            return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private enum Direction
        {
            Up,
            Down
        }

        private bool Move(Direction direction)
        {
            if (!EnsureInitialized())
                return false;

            int position = _textView.Caret.Position.BufferPosition.Position;

            ParseItem item = _tree.StyleSheet.ItemBeforePosition(position);

            Declaration dec = item.FindType<Declaration>();
            if (dec != null)
            {
                return HandleDeclaration(direction, dec);
            }

            Selector selector = item.FindType<Selector>();
            if (selector != null)
            {
                return HandleSelector(direction, selector);
            }

            return false;
        }

        private bool HandleDeclaration(Direction direction, Declaration declaration)
        {
            RuleBlock rule = declaration.FindType<RuleBlock>();
            if (rule == null || rule.Text.IndexOfAny(new[] { '\r', '\n' }) == -1 || (direction == Direction.Up && rule.Declarations.First() == declaration) || (direction == Direction.Down && rule.Declarations.Last() == declaration))
                return false;

            Declaration sibling = null;
            string text = null;

            if (direction == Direction.Up)
            {
                sibling = rule.Declarations.ElementAt(rule.Declarations.IndexOf(declaration) - 1);
                text = declaration.Text + sibling.Text;
            }
            else
            {
                sibling = rule.Declarations.ElementAt(rule.Declarations.IndexOf(declaration) + 1);
                text = sibling.Text + declaration.Text;
            }

            EditorExtensionsPackage.DTE.UndoContext.Open("Move CSS declaration");

            using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
            {
                int start = Math.Min(declaration.Start, sibling.Start);
                int end = Math.Max(declaration.AfterEnd, sibling.AfterEnd);
                edit.Replace(start, end - start, text);
                edit.Apply();

                if (direction == Direction.Up)
                    _textView.Caret.MoveTo(new SnapshotPoint(_textView.TextBuffer.CurrentSnapshot, sibling.Start + 1));

                EditorExtensionsPackage.DTE.ExecuteCommand("Edit.FormatSelection");
                EditorExtensionsPackage.DTE.UndoContext.Close();
            }

            return true;
        }

        private bool HandleSelector(Direction direction, Selector selector)
        {
            //new WriteBrowserXml().Parse();
            RuleSet rule = selector.FindType<RuleSet>();
            if (rule == null)
                return false;

            if (direction == Direction.Up)
            {
                rule = rule.PreviousSibling as RuleSet;
                if (rule == null)
                    return false;
            }

            EditorExtensionsPackage.DTE.UndoContext.Open("Move CSS rule");

            using (ITextEdit edit = _textView.TextBuffer.CreateEdit())
            {
                int position = SwapItemWithNextSibling(rule, edit);
                if (position > -1)
                {
                    if (direction == Direction.Down)
                        _textView.Caret.MoveTo(new SnapshotPoint(_textView.TextBuffer.CurrentSnapshot, position + 1));
                    else
                        _textView.Caret.MoveTo(new SnapshotPoint(_textView.TextBuffer.CurrentSnapshot, rule.Start + 1));

                    // TODO: Format both rules
                    EditorExtensionsPackage.DTE.ExecuteCommand("Edit.FormatSelection");
                }

                EditorExtensionsPackage.DTE.UndoContext.Close();
            }

            return true;
        }

        private int SwapItemWithNextSibling(ParseItem item, ITextEdit edit)
        {
            RuleSet next = item.NextSibling as RuleSet;
            if (next == null)
                return -1;

            ITextSnapshot snapshot = _textView.TextBuffer.CurrentSnapshot;
            string whitespace = snapshot.GetText(item.AfterEnd, next.Start - item.AfterEnd);
            string text = next.Text + whitespace + item.Text;

            edit.Replace(item.Start, next.AfterEnd - item.Start, text);
            edit.Apply();

            return item.Start + next.Length + whitespace.Length;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    switch (prgCmds[i].cmdID)
                    {
                        case 2401: // Up
                        case 2400: // Down
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                    }
                }
            }

            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public bool EnsureInitialized()
        {
            if (_tree == null && Microsoft.Web.Editor.WebEditor.Host != null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_textView.TextBuffer);
                    _tree = document.Tree;
                }
                catch (ArgumentNullException)
                {
                }
            }

            return _tree != null;
        }
    }

    public class WriteBrowserXml
    {
        private const string _fileName = @"C:\Users\madsk\Documents\visual studio 2012\Projects\RealWorldValidator\RealWorldValidator\App_Data\browsers.xml";

        public void Parse()
        {
            ICssSchemaInstance root = CssSchemaManager.SchemaManager.GetSchemaRoot(null);
            IEnumerable<ICssSchemaInstance> schemas = GetAllSchemas(root);
            using (XmlWriter writer = XmlWriter.Create(_fileName))
            {
                writer.WriteStartElement("css");

                // @-Directives
                List<ICssCompletionListEntry> directives = new List<ICssCompletionListEntry>(root.AtDirectives);
                directives.AddRange(schemas.SelectMany(s => s.AtDirectives));
                directives = RemoveDuplicates(directives);

                writer.WriteStartElement("atDirectives");
                WriteSection(writer, directives);
                //WriteSection(writer, root.AtDirectives);
                //foreach (var schema in schemas)
                //    WriteSection(writer, schema.AtDirectives);
                writer.WriteEndElement();

                // Pseudos
                List<ICssCompletionListEntry> pseudos = new List<ICssCompletionListEntry>(root.PseudoClassesAndElements);
                pseudos.AddRange(schemas.SelectMany(s => s.PseudoClassesAndElements));
                pseudos = RemoveDuplicates(pseudos);

                writer.WriteStartElement("pseudoClasses");
                WriteSection(writer, pseudos.Where(p => p.DisplayText[1] != ':'));
                writer.WriteEndElement();

                writer.WriteStartElement("pseudoElements");
                WriteSection(writer, pseudos.Where(p => p.DisplayText[1] == ':'));
                writer.WriteEndElement();

                // Properties
                List<ICssCompletionListEntry> properties = new List<ICssCompletionListEntry>(root.Properties);
                properties.AddRange(schemas.SelectMany(s => s.Properties));
                properties = RemoveDuplicates(properties);

                writer.WriteStartElement("properties");
                WriteProperties(writer, properties, root);
                //WriteProperties(writer, root.Properties, root);
                //foreach (var schema in schemas)
                //    WriteProperties(writer, schema.Properties, schema);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        private List<ICssCompletionListEntry> RemoveDuplicates(List<ICssCompletionListEntry> list)
        {
            for (int i = list.Count() - 1; i > -1; i--)
            {
                if (list.Count(p => p.DisplayText == list.ElementAt(i).DisplayText) > 1)
                    list.RemoveAt(i);
            }

            return list;
        }

        string[] vs = new[] { "@-we-palette", "@unspecified", "@global", "@specific" };

        private IEnumerable<ICssSchemaInstance> GetAllSchemas(ICssSchemaInstance rootSchema)
        {
            foreach (ICssCompletionListEntry directive in rootSchema.AtDirectives)
            {
                if (vs.Contains(directive.DisplayText))
                    continue;

                ICssSchemaInstance schema = rootSchema.GetAtDirectiveSchemaInstance(directive.DisplayText);
                if (schema != null && schema.Properties.Count() != rootSchema.Properties.Count())
                    yield return schema;
            }
        }

        private void WriteSection(XmlWriter writer, IEnumerable<ICssCompletionListEntry> entries)
        {
            foreach (ICssCompletionListEntry entry in entries.OrderBy(e => e.DisplayText))
            {
                writer.WriteStartElement("entry");
                writer.WriteAttributeString("name", entry.DisplayText);
                writer.WriteAttributeString("version", entry.GetAttribute("version"));
                WriteBrowserSupport(writer, entry);

                if (!string.IsNullOrEmpty(entry.GetAttribute("standard-reference")))
                    writer.WriteAttributeString("ref", entry.GetAttribute("standard-reference"));

                if (!string.IsNullOrEmpty(entry.GetAttribute("syntax")))
                    writer.WriteAttributeString("syntax", entry.GetAttribute("syntax"));

                if (!string.IsNullOrEmpty(entry.GetAttribute("description")))
                    writer.WriteElementString("desc", entry.GetAttribute("description"));

                writer.WriteEndElement();
            }
        }

        private void WriteProperties(XmlWriter writer, IEnumerable<ICssCompletionListEntry> entries, ICssSchemaInstance schema)
        {
            foreach (ICssCompletionListEntry entry in entries.OrderBy(e => e.DisplayText))
            {
                writer.WriteStartElement("entry");
                writer.WriteAttributeString("name", entry.DisplayText);
                writer.WriteAttributeString("restriction", entry.GetAttribute("restriction"));
                writer.WriteAttributeString("version", entry.GetAttribute("version"));
                WriteBrowserSupport(writer, entry);

                if (!string.IsNullOrEmpty(entry.GetAttribute("standard-reference")))
                    writer.WriteAttributeString("ref", entry.GetAttribute("standard-reference"));

                if (!string.IsNullOrEmpty(entry.GetAttribute("syntax")))
                    writer.WriteAttributeString("syntax", entry.GetAttribute("syntax"));

                if (!string.IsNullOrEmpty(entry.GetAttribute("description")))
                    writer.WriteElementString("desc", entry.GetAttribute("description"));

                var values = schema.GetPropertyValues(entry.DisplayText);
                if (values.Count() > 2)
                {
                    writer.WriteStartElement("values");
                    foreach (ICssCompletionListEntry value in values.OrderBy(v => v.DisplayText))
                    {
                        if (value.DisplayText == "initial" || value.DisplayText == "inherit")
                            continue;

                        writer.WriteStartElement("value");
                        writer.WriteAttributeString("name", value.DisplayText);
                        writer.WriteAttributeString("version", value.GetAttribute("version") != string.Empty ? value.GetAttribute("version") : entry.GetAttribute("version"));
                        WriteBrowserSupport(writer, value);

                        if (!string.IsNullOrEmpty(value.GetAttribute("description")))
                            writer.WriteElementString("desc", value.GetAttribute("description"));

                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }

        private static void WriteBrowserSupport(XmlWriter writer, ICssCompletionListEntry entry)
        {
            string attr = entry.GetAttribute("browsers");

            if (string.IsNullOrEmpty(attr))
                attr = "all";

            writer.WriteAttributeString("browsers", attr);
        }
    }
}