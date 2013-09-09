using Microsoft.CSS.Core;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public interface IDocument : IDisposable
    {
        void Reparse();
        void Reparse(string text);
        IEnumerable<IStylingRule> Rules { get; }
        string GetSelectorName(RuleSet ruleSet);
        void Import(StyleSheet styleSheet);
    }
}
