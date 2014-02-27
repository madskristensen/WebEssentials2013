using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Language.Intellisense;

namespace MadsKristensen.EditorExtensions.Css
{
    class CssSmartTag : SmartTag
    {
        public CssSmartTag(ReadOnlyCollection<SmartTagActionSet> actionSets)
            : base(SmartTagType.Factoid, actionSets)
        {
        }
    }
}
