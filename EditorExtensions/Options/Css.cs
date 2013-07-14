using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

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
            Settings.SetValue(WESettings.Keys.EnableCssSelectorHighligting, EnableCssSelectorHighligting);
            Settings.SetValue(WESettings.Keys.EnableCssMinification, EnableCssMinification);
            Settings.SetValue(WESettings.Keys.ValidateStarSelector, ValidateStarSelector);
            Settings.SetValue(WESettings.Keys.ValidateOverQualifiedSelector, ValidateOverQualifiedSelector);
            Settings.SetValue(WESettings.Keys.CssErrorLocation, (int)CssErrorLocation);
            Settings.SetValue(WESettings.Keys.SyncVendorValues, SyncVendorValues);
            Settings.SetValue(WESettings.Keys.ShowInitialInherit, ShowInitialInherit);
            Settings.SetValue(WESettings.Keys.ShowUnsupported, ShowUnsupported);
            Settings.SetValue(WESettings.Keys.ShowBrowserTooltip, ShowBrowserTooltip);
            Settings.SetValue(WESettings.Keys.ValidateZeroUnit, ValidateZeroUnit);
            Settings.SetValue(WESettings.Keys.ValidateVendorSpecifics, ValidateVendorSpecifics);
            Settings.SetValue(WESettings.Keys.EnableSpeedTyping, EnableSpeedTyping);

            OnChanged();
            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            EnableCssSelectorHighligting = WESettings.GetBoolean(WESettings.Keys.EnableCssSelectorHighligting);
            EnableCssMinification = WESettings.GetBoolean(WESettings.Keys.EnableCssMinification);
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
            EnableSpeedTyping = WESettings.GetBoolean(WESettings.Keys.EnableSpeedTyping);
        }

        protected void OnChanged()
        {
            CssSchemaManager.SchemaManager.ReloadSchemas();
        }

        [LocDisplayName("Enable selector highlighting")]
        [Description("Highlight matching simple selectors when cursor position changes")]
        [Category("Misc")]
        public bool EnableCssSelectorHighligting { get; set; }

        [LocDisplayName("Minify CSS files on save")]
        [Description("When a .css file (foo.css) is saved and a minified version (foo.min.css) exist, the minified file will be updated. Right-click any .css file to generate .min.css file")]
        [Category("Misc")]
        public bool EnableCssMinification { get; set; }
        
        [LocDisplayName("Enable Speed Typing")]
        [Description("Speed Typing makes it easier to write CSS by eliminating the need for typing curlies, colons and semi-colons.")]
        [Category("Misc")]
        public bool EnableSpeedTyping { get; set; }

        [LocDisplayName("Disallow universal selector")]
        [Description("Disallow the universal, also known as the star selector")]
        [Category("Performance")]
        public bool ValidateStarSelector { get; set; }

        [LocDisplayName("Disallow over qualified ID selector")]
        [Description("Disallow the use of over qualifed ID selectors.")]
        [Category("Performance")]
        public bool ValidateOverQualifiedSelector { get; set; }

        [LocDisplayName("Small images should be inlined")]
        [Description("Small images should be base64 encoded and embedded directly into the stylesheet as dataURIs.")]
        [Category("Performance")]
        public bool ValidateEmbedImages { get; set; }

        [LocDisplayName("Validation location")]
        [Description("Controls where errors are located. To use the 'Errors' output window, select 'Warnings' and change the Visual Studio CSS settings to use 'Errors'")]
        [Category("Validation")]
        public WESettings.Keys.ErrorLocation CssErrorLocation { get; set; }

        [LocDisplayName("Validate vendor specifics")]
        [Description("Validates vendor specific properties, psuedos and @-directives.")]
        [Category("Validation")]
        public bool ValidateVendorSpecifics { get; set; }

        [LocDisplayName("Sync vendor specific values")]
        [Description("Syncronizes vendor specific property values when the standard property is being modified")]
        [Category("Intellisense")]
        public bool SyncVendorValues { get; set; }

        [LocDisplayName("Show initial/inherit")]
        [Description("Shows or hides the global property values 'initial' and 'inherit'. They are still valid to use.")]
        [Category("Intellisense")]
        public bool ShowInitialInherit { get; set; }

        [LocDisplayName("Show unsupported")]
        [Description("Shows the property names, values and pseudos that aren't supported by any browser yet.")]
        [Category("Intellisense")]
        public bool ShowUnsupported { get; set; }

        [LocDisplayName("Show browser support")]
        [Description("Shows the browser support when the mouse is hovering over any CSS property.")]
        [Category("Intellisense")]
        public bool ShowBrowserTooltip { get; set; }

        [LocDisplayName("Disallow units for 0 values")]
        [Description("The value of 0 works without specifying units in all situations where numbers with units or percentages are allowed")]
        [Category("Performance")]
        public bool ValidateZeroUnit { get; set; }
    }
}