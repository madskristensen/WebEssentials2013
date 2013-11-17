using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.ObjectModel;

namespace MadsKristensen.EditorExtensions
{
    class CssSmartTag : SmartTag
    {
        public CssSmartTag(ReadOnlyCollection<SmartTagActionSet> actionSets)
            : base(SmartTagType.Factoid, actionSets)
        {
        }
    }
}
