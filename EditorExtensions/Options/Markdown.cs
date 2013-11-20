using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions
{
    class MarkdownOptions : DialogPage // MarkdownSharp.MarkdownOptions
    {
        public MarkdownOptions()
        {
            Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void SaveSettingsToStorage()
        {
            Settings.SetValue(WESettings.Keys.MarkdownShowPreviewWindow, ShowPreviewWindow);
            Settings.SetValue(WESettings.Keys.MarkdownEnableCompiler, MarkdownEnableCompiler);
            Settings.SetValue(WESettings.Keys.MarkdownCompileToLocation, MarkdownCompileToLocation ?? "");

            Settings.SetValue(WESettings.Keys.MarkdownAutoHyperlinks, MarkdownAutoHyperlinks);
            Settings.SetValue(WESettings.Keys.MarkdownLinkEmails, MarkdownLinkEmails);
            Settings.SetValue(WESettings.Keys.MarkdownAutoNewLine, MarkdownAutoNewLines);
            Settings.SetValue(WESettings.Keys.MarkdownGenerateXHTML, MarkdownGenerateXHTML);
            Settings.SetValue(WESettings.Keys.MarkdownEncodeProblemUrlCharacters, MarkdownEncodeProblemUrlCharacters);
            Settings.SetValue(WESettings.Keys.MarkdownStrictBoldItalic, MarkdownStrictBoldItalic);

            Settings.Save();
        }

        public override void LoadSettingsFromStorage()
        {
            ShowPreviewWindow = WESettings.GetBoolean(WESettings.Keys.MarkdownShowPreviewWindow);
            MarkdownEnableCompiler = WESettings.GetBoolean(WESettings.Keys.MarkdownEnableCompiler);
            MarkdownCompileToLocation = WESettings.GetString(WESettings.Keys.MarkdownCompileToLocation);

            MarkdownAutoHyperlinks = WESettings.GetBoolean(WESettings.Keys.MarkdownAutoHyperlinks);
            MarkdownLinkEmails = WESettings.GetBoolean(WESettings.Keys.MarkdownLinkEmails);
            MarkdownAutoNewLines = WESettings.GetBoolean(WESettings.Keys.MarkdownAutoNewLine);
            MarkdownGenerateXHTML = WESettings.GetBoolean(WESettings.Keys.MarkdownGenerateXHTML);
            MarkdownEncodeProblemUrlCharacters = WESettings.GetBoolean(WESettings.Keys.MarkdownEncodeProblemUrlCharacters);
            MarkdownStrictBoldItalic = WESettings.GetBoolean(WESettings.Keys.MarkdownStrictBoldItalic);
        }

        [LocDisplayName("Show preview window")]
        [Description("Shows the preview window when editing a Markdown file.")]
        [Category("Preview")]
        [DefaultValue(true)]
        public bool ShowPreviewWindow { get; set; }

        [LocDisplayName("Save compiled Markdown files to disk")]
        [Description("Enables saving Markdown files to disk. When false, no generated HTML files will be saved to disk, including during a build.")]
        [Category("Compile")]
        [DefaultValue(false)]
        public bool MarkdownEnableCompiler { get; set; }

        [LocDisplayName("Save output to a custom folder")]
        [Description("Saves each Markdown file into a custom folder. Compiled Markdown files (.html) are not saved by default. Leave empty to save the compiled .html file to the same directory as the .md file. Or, prefix your output directory with a `/` to indicate that it starts at the project's root directory (for example '/html') - this will apply to ALL .md files! Otherwise, a relative path is assumed (starting from the file being compiled) - this may cause the output path to be different for each .html file compiled. Leave empty to effectively disable this option, nothing is saved to disk.")]
        [Category("Compile")]
        [DefaultValue("")]
        public string MarkdownCompileToLocation { get; set; }

        [LocDisplayName("Make bare Urls into hyperlinks")]
        [Description("When true, (most) bare plain Urls are auto-hyperlinked. WARNING: this is a significant deviation from the markdown spec.")]
        [Category("Compile Options")]
        [DefaultValue(false)]
        public bool MarkdownAutoHyperlinks { get; set; }

        [LocDisplayName("Make bare emails into links")]
        [Description("When false, email addresses will never be auto-linked. WARNING: this is a significant deviation from the markdown spec.")]
        [Category("Compile Options")]
        [DefaultValue(false)]
        public bool MarkdownLinkEmails { get; set; }

        [LocDisplayName("Make return into a newline")]
        [Description("When true, RETURN becomes a literal newline. WARNING: this is a significant deviation from the markdown spec.")]
        [Category("Compile Options")]
        [DefaultValue(false)]
        public bool MarkdownAutoNewLines { get; set; }

        [LocDisplayName("Generate XHTML output")]
        [Description("When true, the output is valid XHTML. Otherwise regular HTML it output. In this case, when true (mostly) means that single tags are closed with `/>` instead of `>`.")]
        [Category("Compile Options")]
        [DefaultValue(true)]
        public bool MarkdownGenerateXHTML { get; set; }

        [LocDisplayName("Encode problem Url characters")]
        [Description("When true, problematic Url characters like [, ], (, and so forth will be encoded. WARNING: this is a significant deviation from the markdown spec.")]
        [Category("Compile Options")]
        [DefaultValue(false)]
        public bool MarkdownEncodeProblemUrlCharacters { get; set; }

        [LocDisplayName("Require non-word characters for bold/italic")]
        [Description("When true, bold and italic require non-word characters on both sides. WARNING: this is a significant deviation from the markdown spec.")]
        [Category("Compile Options")]
        [DefaultValue(false)]
        public bool MarkdownStrictBoldItalic { get; set; }
    }
}
