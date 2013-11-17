using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    interface ICssSmartTagProvider
    {
        Type ItemType { get; }
        IEnumerable<ISmartTagAction> GetSmartTagActions(ParseItem item, int caretPosition, ITrackingSpan itemTrackingSpan, ITextView view);
    }
}
