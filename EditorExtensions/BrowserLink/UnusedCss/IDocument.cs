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
        bool SnapshotOnChange { get; set; }
        string GetSelectorName(RuleSet ruleSet);
    }
}
