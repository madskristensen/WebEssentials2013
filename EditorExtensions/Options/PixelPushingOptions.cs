using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class PixelPushingOptions : DialogPage
    {
        [LocDisplayName("Enable Pixel Pushing mode by default")]
        [Description("Whether or not Pixel Pushing mode is turned on by default")]
        [Category("CSS")]
        public bool IsOnByDefault { get; set; }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.PixelPushing_OnByDefault, IsOnByDefault);
            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            IsOnByDefault = WESettings.GetBoolean(WESettings.Keys.PixelPushing_OnByDefault);
        }


        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            if (SettingsUpdated != null)
            {
                SettingsUpdated(null, null);
            }
        }

        public static event EventHandler SettingsUpdated;
    }
}
