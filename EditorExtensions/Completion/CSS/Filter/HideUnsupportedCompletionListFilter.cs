using System;
using System.ComponentModel.Composition;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSchemaFilterProvider))]
    [Name("HideUnsupportedSchemaFilterProvider")]
    internal class HideUnsupportedSchemaFilterProvider : ICssSchemaFilterProvider
    {
        public ICssSchemaFilter CreateFilter(ICssSchemaManager schemaManager, ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new HideUnsupportedSchemaFilter());
        }
    }

    internal class HideUnsupportedSchemaFilter : ICssSchemaFilter
    {
        public bool IsSupported(Version cssVersion, ICssCompletionListEntry entry)
        {
            if (WESettings.Instance.Css.ShowUnsupported)
                return entry.IsSupported(cssVersion);

            return entry.GetAttribute("browsers") != "none" || entry.DisplayText.Contains("gradient");
        }

        public string Name
        {
            get { return WESettings.Instance.Css.ShowUnsupported ? string.Empty : "WE"; }
        }

        public bool Equals(ICssSchemaFilter other)
        {
            return other.Name.Equals(Name);
        }
    }
}