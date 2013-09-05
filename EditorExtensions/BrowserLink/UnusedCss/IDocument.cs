using System;
using System.Collections.Generic;
namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public interface IDocument : IDisposable
    {
        void Reparse();
        void Reparse(string text);
        IEnumerable<IStylingRule> Rules { get; }

    }
}
