using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("EmbedImagesErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class EmbedImagesErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            UrlItem url = (UrlItem)item;

            if (!WESettings.GetBoolean(WESettings.Keys.ValidateEmbedImages) || !url.IsValid || url.UrlString.Text.Contains("base64,") || context == null)
                return ItemCheckResult.Continue;

            string fileName = ImageQuickInfo.GetFileName(url.UrlString.Text);
            if (fileName.Contains("://"))
                return ItemCheckResult.Continue;

            FileInfo file = new FileInfo(fileName);

            if (file.Exists && file.Length < (1024 * 3))
            {
                Declaration dec = url.FindType<Declaration>();
                if (dec != null && dec.PropertyName != null && dec.PropertyName.Text[0] != '*' && dec.PropertyName.Text[0] != '_')
                {
                    string error = string.Format(Resources.PerformanceEmbedImageAsDataUri, file.Length);
                    context.AddError(new SimpleErrorTag(url.UrlString, error));
                }
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(UrlItem) }; }
        }
    }
}