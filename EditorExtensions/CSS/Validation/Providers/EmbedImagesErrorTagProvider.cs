using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Css
{
    [Export(typeof(ICssItemChecker))]
    [Name("EmbedImagesErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class EmbedImagesErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            UrlItem url = (UrlItem)item;

            if (!WESettings.Instance.Css.ValidateEmbedImages || !url.IsValid || url.UrlString == null || url.UrlString.Text.Contains("base64,") || context == null)
                return ItemCheckResult.Continue;

            string fileName = ImageQuickInfo.GetFullUrl(url.UrlString.Text, WebEssentialsPackage.DTE.ActiveDocument.FullName);
            if (string.IsNullOrEmpty(fileName) || fileName.Contains("://"))
                return ItemCheckResult.Continue;

            // Remove parameters if any; c:/temp/myfile.ext?#iefix
            fileName = fileName.Split('?', '#')[0];

            FileInfo file = new FileInfo(fileName);

            if (file.Exists && file.Length < (1024 * 3))
            {
                Declaration dec = url.FindType<Declaration>();
                if (dec != null && dec.PropertyName != null && dec.PropertyName.Text[0] != '*' && dec.PropertyName.Text[0] != '_')
                {
                    string error = string.Format(CultureInfo.CurrentCulture, Resources.PerformanceEmbedImageAsDataUri, file.Length);
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