using System;
using Microsoft.CSS.Core;
namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public interface IStylingRule : IEquatable<IStylingRule>
    {
        int Column { get; }
        string DisplaySelectorName { get; }
        string File { get; }
        int Length { get; }
        int SelectorLength { get; }
        int Line { get; }
        int Offset { get; }
        bool Matches(RuleSet rule);
        RuleSet Source { get; }
        string CleansedSelectorName { get; }

        bool IsMatch(string standardizedSelectorText);
    }
}
