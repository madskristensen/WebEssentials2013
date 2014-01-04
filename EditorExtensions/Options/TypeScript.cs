using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class TypeScriptOptions : DialogPage
    {
        public TypeScriptOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.ShowTypeScriptPreviewWindow, ShowTypeScriptPreviewWindow);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            ShowTypeScriptPreviewWindow = WESettings.GetBoolean(WESettings.Keys.ShowTypeScriptPreviewWindow);
        }

        [LocDisplayName("Show preview pane")]
        [Description("Shows the preview pane when editing a TypeScript file.")]
        [Category("TypeScript")]
        [DefaultValue(true)]
        public bool ShowTypeScriptPreviewWindow { get; set; }
    }
}
