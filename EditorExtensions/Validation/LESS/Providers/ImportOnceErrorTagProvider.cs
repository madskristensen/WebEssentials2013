using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("ImportOnceErrorTagProvider")]
    internal class ImportOnceErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            var directive = (ImportDirective)item;

            if (!directive.IsValid || context == null)
                return ItemCheckResult.Continue;

            if (directive.Keyword.Text == "import-once")
            {
                ICssError tag = new SimpleErrorTag(directive.Keyword, Resources.LessImportOnceDeprecated);
                context.AddError(tag);
                return ItemCheckResult.CancelCurrentItem;
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(ImportDirective) }; }
        }
    }
}
