using System;
using System.Collections.Generic;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public interface IDocument : IDisposable
    {
        void Reparse();
        void Reparse(string text);
        IEnumerable<IStylingRule> Rules { get; }
        bool IsProcessingUnusedCssRules { get; set; }
        string GetSelectorName(RuleSet ruleSet);
        object ParseSync { get; }
        string FileName { get; }
    }
}
