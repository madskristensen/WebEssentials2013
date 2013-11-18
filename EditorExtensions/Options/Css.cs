using System.ComponentModel;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class CssOptions : DialogPage
    {
        public CssOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.EnableCssMinification, EnableCssMinification);
            Settings.SetValue(WESettings.Keys.CssEnableGzipping, EnableGzipping);
            Settings.SetValue(WESettings.Keys.ValidateStarSelector, ValidateStarSelector);
            Settings.SetValue(WESettings.Keys.ValidateOverQualifiedSelector, ValidateOverQualifiedSelector);
            Settings.SetValue(WESettings.Keys.CssErrorLocation, (int)CssErrorLocation);
            Settings.SetValue(WESettings.Keys.SyncVendorValues, SyncVendorValues);
            Settings.SetValue(WESettings.Keys.ShowInitialInherit, ShowInitialInherit);
            Settings.SetValue(WESettings.Keys.ShowUnsupported, ShowUnsupported);
            Settings.SetValue(WESettings.Keys.ShowBrowserTooltip, ShowBrowserTooltip);
            Settings.SetValue(WESettings.Keys.ValidateZeroUnit, ValidateZeroUnit);
            Settings.SetValue(WESettings.Keys.ValidateVendorSpecifics, ValidateVendorSpecifics);

            OnChanged();
            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableCssMinification = WESettings.GetBoolean(WESettings.Keys.EnableCssMinification);
            EnableGzipping = WESettings.GetBoolean(WESettings.Keys.CssEnableGzipping);
            ValidateStarSelector = WESettings.GetBoolean(WESettings.Keys.ValidateStarSelector);
            ValidateOverQualifiedSelector = WESettings.GetBoolean(WESettings.Keys.ValidateOverQualifiedSelector);
            CssErrorLocation = (WESettings.Keys.ErrorLocation)WESettings.GetInt(WESettings.Keys.CssErrorLocation);
            SyncVendorValues = WESettings.GetBoolean(WESettings.Keys.SyncVendorValues);
            ShowInitialInherit = WESettings.GetBoolean(WESettings.Keys.ShowInitialInherit);
            ShowUnsupported = WESettings.GetBoolean(WESettings.Keys.ShowUnsupported);
            ValidateEmbedImages = WESettings.GetBoolean(WESettings.Keys.ValidateEmbedImages);
            ShowBrowserTooltip = WESettings.GetBoolean(WESettings.Keys.ShowBrowserTooltip);
            ValidateZeroUnit = WESettings.GetBoolean(WESettings.Keys.ValidateZeroUnit);
            ValidateVendorSpecifics = WESettings.GetBoolean(WESettings.Keys.ValidateVendorSpecifics);
        }

        protected void OnChanged()
        {
            CssSchemaManager.SchemaManager.ReloadSchemas();
        }

        [LocDisplayName("Minify CSS files on save")]
        [Description("When a .css file (foo.css) is saved and a minified version (foo.min.css) exist, the minified file will be updated. Right-click any .css file to generate .min.css file")]
        [Category("Misc")]
        public bool EnableCssMinification { get; set; }

        [LocDisplayName("Gzip minified CSS files on save")]
        [Description("When a .css file (foo.css) is saved and a minified version (foo.min.css) exist, a gzipped version of the file will be created.")]
        [Category("Misc")]
        public bool EnableGzipping { get; set; }

        [LocDisplayName("Disallow universal selector")]
        [Description("Disallows the universal selector (*).")]
        [Category("Performance")]
        public bool ValidateStarSelector { get; set; }

        [LocDisplayName("Disallow over-qualified ID selector")]
        [Description("Disallows the use of overly qualified ID selectors.")]
        [Category("Performance")]
        public bool ValidateOverQualifiedSelector { get; set; }

        [LocDisplayName("Small images should be inlined")]
        [Description("Warns when small images are not base64 encoded and embedded directly into the stylesheet as dataURIs.")]
        [Category("Performance")]
        public bool ValidateEmbedImages { get; set; }

        [LocDisplayName("Validation location")]
        [Description("Controls where errors are located. To use the 'Errors' output window, select 'Warnings' and change the Visual Studio CSS settings to use 'Errors'.")]
        [Category("Validation")]
        public WESettings.Keys.ErrorLocation CssErrorLocation { get; set; }

        [LocDisplayName("Validate vendor specifics")]
        [Description("Validates vendor specific properties, psuedos and @-directives.")]
        [Category("Validation")]
        public bool ValidateVendorSpecifics { get; set; }

        [LocDisplayName("Sync vendor specific values")]
        [Description("Synchronizes vendor specific property values when the standard property is being modified.")]
        [Category("Intellisense")]
        public bool SyncVendorValues { get; set; }

        [LocDisplayName("Show initial/inherit")]
        [Description("Shows or hides the global property values 'initial' and 'inherit'. They are still valid to use.")]
        [Category("Intellisense")]
        public bool ShowInitialInherit { get; set; }

        [LocDisplayName("Show unsupported")]
        [Description("Shows property names, values and pseudos that aren't supported by any browser yet.")]
        [Category("Intellisense")]
        public bool ShowUnsupported { get; set; }

        [LocDisplayName("Show browser support")]
        [Description("Shows the browser support when the mouse is hovering over any CSS property.")]
        [Category("Intellisense")]
        public bool ShowBrowserTooltip { get; set; }

        [LocDisplayName("Disallow units for 0 values")]
        [Description("Warns when units are unnecessarily specified for the number 0 (which never needs a unit in CSS).")]
        [Category("Performance")]
        public bool ValidateZeroUnit { get; set; }
    }
}