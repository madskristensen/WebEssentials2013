using System;
using System.IO;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CssRule : IStylingRule
    {
        public CssRule(string sourceFile, string fileText, RuleSet ruleSet, IDocument document)
        {
            SelectorName = document.GetSelectorName(ruleSet);
            CleansedSelectorName = RuleRegistry.StandardizeSelector(SelectorName);
            DisplaySelectorName = SelectorName.Replace('\r', '\n').Replace("\n", "").Trim();
            File = sourceFile;
            int line, column;
            CalculateLineAndColumn(fileText, ruleSet, out line, out column);
            Line = line;
            Column = column;
            Offset = ruleSet.Range.Start;
            Length = ruleSet.Range.Length;
        }

        public string DisplaySelectorName { get; private set; }

        public string CleansedSelectorName { get; private set; }

        public int Column { get; private set; }

        public string File { get; private set; }

        public int Length { get; private set; }

        public int Line { get; private set; }

        public int Offset { get; private set; }

        public string SelectorName { get; private set; }

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
            var offset = ruleSet.Start;
            var textSoFar = fileText.Substring(0, offset);

            using (var reader = new StringReader(textSoFar))
            {
                var lineCount = 0;
                var lastLine = "";
                string currentLine;

                while ((currentLine = reader.ReadLine()) != null)
                {
                    lastLine = currentLine;
                    ++lineCount;
                }

                lineNumber = lineCount + 1;
                columnNumber = lastLine.Length;
            }
        }


        public bool IsMatch(string standardizedSelectorText)
        {
            return string.Equals(CleansedSelectorName, standardizedSelectorText, StringComparison.Ordinal);
        }
    }
}