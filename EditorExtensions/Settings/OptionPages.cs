using System;
using ConfOxide;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.Settings
{
    abstract class SettingsOptionPage<T> : DialogPage where T : SettingsBase<T>
    {
        ///<summary>Gets the actual settings instance read by the application.</summary>
        public T OriginalTarget { get; private set; }
        ///<summary>Gets a clone of the settings instance used to bind the options pages.</summary>
        public T DialogTarget { get; private set; }
        public override object AutomationObject { get { return DialogTarget; } }

        protected SettingsOptionPage(Func<WESettings, T> targetSelector) : this(targetSelector(WESettings.Instance)) { }
        private SettingsOptionPage(T target)
        {
            OriginalTarget = target;
            DialogTarget = target.CreateCopy();
            //TODO: Update copy when dialog is shown?
            //Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void ResetSettings()
        {
            DialogTarget.ResetValues();
        }

        public override void LoadSettingsFromStorage()
        {
            DialogTarget.AssignFrom(OriginalTarget);
        }
        public override void SaveSettingsToStorage()
        {
            OriginalTarget.AssignFrom(DialogTarget);
            SettingsStore.Save();
        }
    }
    class SpriteOptions : SettingsOptionPage<SpriteSettings>
    {
        public SpriteOptions() : base(s => s.Sprite) { }
    }
    class TypeScriptOptions : SettingsOptionPage<TypeScriptSettings>
    {
        public TypeScriptOptions() : base(s => s.TypeScript) { }
    }
    class CodeGenOptions : SettingsOptionPage<CodeGenSettings>
    {
        public CodeGenOptions() : base(s => s.CodeGen) { }
    }
    class JavaScriptOptions : SettingsOptionPage<JavaScriptSettings>
    {
        public JavaScriptOptions() : base(s => s.JavaScript) { }
    }
    class GeneralOptions : SettingsOptionPage<GeneralSettings>
    {
        public GeneralOptions() : base(s => s.General) { }
    }
    class HtmlOptions : SettingsOptionPage<HtmlSettings>
    {
        public HtmlOptions() : base(s => s.Html) { }
    }
    class CssOptions : SettingsOptionPage<CssSettings>
    {
        public CssOptions() : base(s => s.Css) { }
    }

    class CoffeeScriptOptions : SettingsOptionPage<CoffeeScriptSettings>
    {
        public CoffeeScriptOptions() : base(s => s.CoffeeScript) { }
    }
    class LiveScriptOptions : SettingsOptionPage<LiveScriptSettings>
    {
        public LiveScriptOptions() : base(s => s.LiveScript) { }
    }
    class LessOptions : SettingsOptionPage<LessSettings>
    {
        public LessOptions() : base(s => s.Less) { }
    }
    class ScssOptions : SettingsOptionPage<ScssSettings>
    {
        public ScssOptions() : base(s => s.Scss) { }
    }
    class MarkdownOptions : SettingsOptionPage<MarkdownSettings>
    {
        public MarkdownOptions() : base(s => s.Markdown) { }
    }
    class SweetJsOptions : SettingsOptionPage<SweetJsSettings>
    {
        public SweetJsOptions() : base(s => s.SweetJs) { }
    }

    class BrowserLinkOptions : SettingsOptionPage<BrowserLinkSettings>
    {
        public BrowserLinkOptions() : base(s => s.BrowserLink) { }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            // This event is used to re-initialize BrowserLink stuff.
            // TODO: Move to SettingsStore class?
            if (SettingsUpdated != null)
                SettingsUpdated(this, e);
        }

        public static event EventHandler SettingsUpdated;
    }

}
