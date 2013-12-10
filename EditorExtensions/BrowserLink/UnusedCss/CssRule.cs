using System;
using System.Linq;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CssRule : IStylingRule
    {
        private readonly RuleSet _ruleSet;

        public string DisplaySelectorName { get; private set; }
        public string CleansedSelectorName { get; private set; }
        public int Column { get; private set; }
        public string File { get; private set; }
        public int Length { get; private set; }
        public int Line { get; private set; }
        public int Offset { get; private set; }
        public string SelectorName { get; private set; }
        public RuleSet Source { get { return _ruleSet; } }
        public int SelectorLength { get; private set; }

        private CssRule(string sourceFile, string fileText, RuleSet ruleSet, string selectorName)
        {
            SelectorName = selectorName;
            CleansedSelectorName = RuleRegistry.StandardizeSelector(SelectorName);
            DisplaySelectorName = SelectorName.Replace('\r', '\n').Replace("\n", "").Trim();

            string oldDisplaySelectorName = null;

            while (DisplaySelectorName != oldDisplaySelectorName)
            {
                oldDisplaySelectorName = DisplaySelectorName;
                DisplaySelectorName = DisplaySelectorName.Replace("  ", " ");
            }

            File = sourceFile;

            int line, column;

            CalculateLineAndColumn(fileText, ruleSet, out line, out column);
            Line = line;
            Column = column;
            Offset = ruleSet.Range.Start;
            Length = ruleSet.Range.Length;

            var lastSelector = ruleSet.Selectors[ruleSet.Selectors.Count - 1];

            SelectorLength = lastSelector.Length + lastSelector.Start - ruleSet.Selectors[0].Start;
            _ruleSet = ruleSet;
        }

        public static IStylingRule From(string sourceFile, string fileText, RuleSet ruleSet, IDocument document)
        {
            var selectorName = document.GetSelectorName(ruleSet);

            if (selectorName == null)
            {
                return null;
            }

            try
            {
                return new CssRule(sourceFile, fileText, ruleSet, selectorName);
            }
            catch
            {
                //If anything went wrong in creating the rule reference, don't return it, it'll get cataloged at the next opportunity, presumably when the document is valid again
                return null;
            }
        }

        public bool Equals(IStylingRule other)
        {
            return !ReferenceEquals(other, null) && other.File == File && other.IsMatch(CleansedSelectorName) && other.Line == Line && other.Column == Column && other.Offset == Offset && other.Length == Length;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CssRule);
        }

        public override int GetHashCode()
        {
            return File.GetHashCode() ^ CleansedSelectorName.GetHashCode() ^ Line ^ Column ^ Offset ^ Length;
        }

        public override string ToString()
        {
            return DisplaySelectorName + " (" + File + ")";
        }

        private static void CalculateLineAndColumn(string fileText, RuleSet ruleSet, out int lineNumber, out int columnNumber)
        {
            var leadingContent = fileText.Substring(0, ruleSet.Start)
                .Split(new[] { "\r\n" }, StringSplitOptions.None)
                .Select(x => x.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
                .SelectMany(x => x)
                .ToArray();

            if (leadingContent.Length == 0)
            {
                lineNumber = 1;
                columnNumber = 1;
                return;
            }

            lineNumber = leadingContent.Length;
            columnNumber = leadingContent[leadingContent.Length - 1].Length;
        }

        public bool IsMatch(string standardizedSelectorText)
        {
            return string.Equals(CleansedSelectorName, standardizedSelectorText, StringComparison.Ordinal);
        }

        public bool Matches(RuleSet rule)
        {
            return rule.Text == _ruleSet.Text;
        }
    }
}