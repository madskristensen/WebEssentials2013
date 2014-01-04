using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class LessExtractVariableSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private ParseItem _item;

        public LessExtractVariableSmartTagAction(ITrackingSpan span, ParseItem item)
        {
            _span = span;
            _item = item;

            if (Icon == null)
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/extract.png", UriKind.RelativeOrAbsolute));
            }
        }

        public override string DisplayText
        {
            get { return "Extract all to variable"; }
        }

        public override void Invoke()
        {
            ParseItem rule = LessExtractVariableCommandTarget.FindParent(_item);
            string text = _item.Text;
            string name = Microsoft.VisualBasic.Interaction.InputBox("Name of the variable", "Web Essentials");

            if (!string.IsNullOrEmpty(name))
            {
                using (EditorExtensionsPackage.UndoContext((DisplayText)))
                {
                    foreach (ParseItem item in FindItems())
                    {
                        Span span = new Span(item.Start, item.Length);
                        _span.TextBuffer.Replace(span, "@" + name);
                    }

                    _span.TextBuffer.Insert(rule.Start, "@" + name + ": " + text + ";" + Environment.NewLine + Environment.NewLine);
                }
            }
        }

        private IList<ParseItem> FindItems()
        {
            if (_item is HexColorValue)
            {
                var visitor = new CssItemCollector<HexColorValue>();
                _item.StyleSheet.Accept(visitor);
                return visitor.Items.Where(i => i.Text == _item.Text).ToArray();
            }
            else if (_item is FunctionColor)
            {
                var visitor = new CssItemCollector<FunctionColor>();
                _item.StyleSheet.Accept(visitor);
                return visitor.Items.Where(i => i.Text == _item.Text).ToArray();
            }
            else if (_item is TokenItem)
            {
                var visitor = new CssItemCollector<TokenItem>();
                _item.StyleSheet.Accept(visitor);
                return visitor.Items.Where(i => i.TokenType == CssTokenType.Identifier && i.Text == _item.Text && i.FindType<Declaration>() != null).ToArray();
            }

            return null;
        }
    }
}
