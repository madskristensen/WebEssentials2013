using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class DeclarationQuickInfo : IQuickInfoSource
    {
        private ITextBuffer _buffer;
        private static readonly string[] browserAbbr = new[] { "C", "FF", "IE", "O", "S" };
        private ICssSchemaInstance _rootSchema;

        public DeclarationQuickInfo(ITextBuffer buffer)
        {
            _buffer = buffer;
            _rootSchema = CssSchemaManager.SchemaManager.GetSchemaRootForBuffer(buffer);
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (session == null || qiContent == null || qiContent.Count > 0 || !WESettings.GetBoolean(WESettings.Keys.ShowBrowserTooltip))
                return;

            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            var tree = CssEditorDocument.FromTextBuffer(_buffer);
            ParseItem item = tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            ParseItem theOne = null;
            ICssCompletionListEntry entry = null;
            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(_rootSchema, item);

            // Declaration
            Declaration dec = item.FindType<Declaration>();
            if (dec != null && dec.PropertyName != null && dec.PropertyName.ContainsRange(point.Value.Position, 1))
            {
                entry = schema.GetProperty(dec.PropertyName.Text);
                theOne = dec.PropertyName;
            }
            else if (dec != null && dec.IsValid && dec.Values.TextStart <= point.Value.Position && dec.Values.TextAfterEnd >= point.Value.Position)
            {
                entry = schema.GetProperty(dec.PropertyName.Text);
                if (entry != null)
                {
                    var list = schema.GetPropertyValues(entry.DisplayText);
                    theOne = dec.StyleSheet.ItemFromRange(point.Value.Position, 0);
                    entry = list.SingleOrDefault(r => r.DisplayText.Equals(theOne.Text, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Pseudo class
            if (entry == null)
            {
                PseudoClassSelector pseudoClass = item.FindType<PseudoClassSelector>();
                if (pseudoClass != null)
                {
                    entry = schema.GetPseudo(pseudoClass.Text);
                    theOne = pseudoClass;
                }
            }

            // Pseudo class function
            if (entry == null)
            {
                PseudoClassFunctionSelector pseudoClassFunction = item.FindType<PseudoClassFunctionSelector>();
                if (pseudoClassFunction != null)
                {
                    entry = schema.GetPseudo(pseudoClassFunction.Text);
                    theOne = pseudoClassFunction;
                }
            }

            // Pseudo element
            if (entry == null)
            {
                PseudoElementSelector pseudoElement = item.FindType<PseudoElementSelector>();
                if (pseudoElement != null)
                {
                    entry = schema.GetPseudo(pseudoElement.Text);
                    theOne = pseudoElement;
                }
            }

            // Pseudo element function
            if (entry == null)
            {
                PseudoElementFunctionSelector pseudoElementFunction = item.FindType<PseudoElementFunctionSelector>();
                if (pseudoElementFunction != null)
                {
                    entry = schema.GetPseudo(pseudoElementFunction.Text);
                    theOne = pseudoElementFunction;
                }
            }

            // @-directive
            if (entry == null)
            {
                AtDirective atDirective = item.Parent as AtDirective;
                if (atDirective != null && atDirective.Keyword != null)
                {
                    entry = schema.GetAtDirective("@" + atDirective.Keyword.Text);
                    theOne = atDirective.Keyword;
                }
            }

            var ruleSet = item.FindType<RuleSet>();

            //If the selector's full name would require computation (it's nested), compute it and add it to the output
            if (ruleSet != null && ruleSet.Parent.FindType<RuleSet>() != null)
            {
                qiContent.Add(LessDocument.GetLessSelectorName(ruleSet));
            }

            if (entry != null)
            {
                applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(theOne.Start, theOne.Length, SpanTrackingMode.EdgeNegative);

                string syntax = entry.GetSyntax(schema.Version);
                string b = entry.GetAttribute("browsers");

                if (string.IsNullOrEmpty(b) && theOne.Parent != null && theOne.Parent is Declaration)
                {
                    b = schema.GetProperty(((Declaration)theOne.Parent).PropertyName.Text).GetAttribute("browsers");
                    if (string.IsNullOrEmpty(syntax))
                        syntax = theOne.Text;
                }

                if (!string.IsNullOrEmpty(syntax))
                {
                    //var example = CreateExample(syntax);
                    qiContent.Add("Example: " + syntax);
                }

                Dictionary<string, string> browsers = GetBrowsers(b);
                qiContent.Add(CreateBrowserList(browsers));
            }
        }

        public static Dictionary<string, string> GetBrowsers(string browsersRaw)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(browsersRaw))
            {
                string[] array = browsersRaw.Split(',');

                foreach (string browserString in array)
                {
                    var browser = GetBrowserVersion(browserString);
                    if (!dic.ContainsKey(browser.Key))
                        dic.Add(browser.Key, browser.Value);
                }
            }
            else
            {
                foreach (string name in browserAbbr)
                {
                    dic.Add(name, "all");
                }
            }

            return dic;
        }

        private static KeyValuePair<string, string> GetBrowserVersion(string browserString)
        {
            var nameChars = browserString.Where(b => char.IsLetter(b)).ToArray();

            string name = string.Join(string.Empty, nameChars);
            string value = "all";

            if (nameChars.Length < browserString.Length)
                value = browserString.Substring(nameChars.Length);

            int index = value.IndexOf('-');
            if (index > -1)
                value = value.Substring(0, index);

            //if (value.StartsWith("1") || value.StartsWith("2") || value.StartsWith("3"))
            //    value = "all";

            return new KeyValuePair<string, string>(name, value);
        }

        private static UIElement CreateBrowserList(Dictionary<string, string> browsers)
        {
            StackPanel panel = new System.Windows.Controls.StackPanel();
            panel.Orientation = Orientation.Horizontal;

            foreach (string name in browserAbbr)
            {
                StackPanel p = new StackPanel();
                p.Orientation = Orientation.Vertical;
                p.Margin = new Thickness(5, 0, 5, 0);
                p.HorizontalAlignment = HorizontalAlignment.Right;

                Image image = new Image();
                image.Height = 24;
                image.Width = 24;
                p.Children.Add(image);

                TextBlock block = new TextBlock();
                block.TextAlignment = TextAlignment.Center;
                block.Background = new SolidColorBrush(Brushes.WhiteSmoke.Color);
                block.Background.Opacity = 0.6;
                block.Margin = new Thickness(0, -7, 0, 0);
                block.HorizontalAlignment = HorizontalAlignment.Right;
                block.FontSize = 11;
                block.FontFamily = new FontFamily("Consolas");

                if (!browsers.ContainsKey(name) && !browsers.ContainsKey("all"))
                {
                    image.Opacity = 0.4;
                    image.Source = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Browsers/" + name + "_gray.png", UriKind.RelativeOrAbsolute));
                }
                else
                {
                    block.Text = browsers.ContainsKey("all") ? string.Empty : browsers[name];
                    image.Source = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Browsers/" + name + ".png", UriKind.RelativeOrAbsolute));
                }

                if (block.Text != "all")
                    p.Children.Add(block);

                panel.Children.Add(p);
            }

            return panel;
        }

        private bool m_isDisposed;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }
}
