using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using MadsKristensen.EditorExtensions.Css;
using Microsoft.Css.Extensions;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions.Less
{
    internal class LessExtractVariableSmartTagAction : CssSmartTagActionBase
    {
        private ITrackingSpan _span;
        private ParseItem _item;
        private string _variableToken;

        public LessExtractVariableSmartTagAction(ITrackingSpan span, ParseItem item, string variableToken)
        {
            _span = span;
            _item = item;
            _variableToken = variableToken;

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
            ParseItem rule = FindParent(_item);
            string text = _item.Text;
            string name = Microsoft.VisualBasic.Interaction.InputBox("Name of the variable", "Web Essentials");

            if (!string.IsNullOrEmpty(name))
            {
                using (WebEssentialsPackage.UndoContext((DisplayText)))
                {
                    foreach (ParseItem item in FindItems())
                    {
                        Span span = new Span(item.Start, item.Length);
                        _span.TextBuffer.Replace(span, _variableToken + name);
                    }

                    _span.TextBuffer.Insert(rule.Start, _variableToken + name + ": " + text + ";" + Environment.NewLine + Environment.NewLine);
                }
            }
        }

        private static ParseItem FindParent(ParseItem item)
        {
            ParseItem parent = item.Parent;

            while (true)
            {
                if (parent.Parent == null || parent.Parent is CssStyleSheet || parent.Parent is AtDirective)
                    break;

                parent = parent.Parent;
            }

            return parent;
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
