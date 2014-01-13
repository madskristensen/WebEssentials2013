
using System.Xml.Linq;

namespace MadsKristensen.EditorExtensions
{
    ///<summary>Migrates settings from legacy XML settings files to the new ConfOxide-based settings objects.</summary>
    public class SettingsMigrator
    {
        readonly XDocument sourceFile;
        readonly XElement settingsElement;
        public SettingsMigrator(string sourcePath) : this(XDocument.Load(sourcePath)) { }
        public SettingsMigrator(XDocument source)
        {
            sourceFile = source;
            settingsElement = source.Root.Element("settings");
        }

        public void ApplyTo(WESettings target)
        {
            target.CodeGen.CamelCasePropertyNames = GetBoolean("JavaScriptCamelCasePropertyNames");
            target.CodeGen.CamelCaseTypeNames = GetBoolean("JavaScriptCamelCaseClassNames");
            // General
            target.General.KeepImportantComments = GetBoolean("KeepImportantComments");
            target.General.AllMessagesToOutputWindow = GetBoolean("AllMessagesToOutputWindow");

            // HTML
            target.Html.EnableEnterFormat = GetBoolean("EnableEnterFormat");
            target.Html.EnableAngularValidation = GetBoolean("EnableAngularValidation");
            target.Html.MinifyOnSave = GetBoolean("HtmlEnableMinification");

            // LESS
            target.Less.CompileOnSave = GetBoolean("LessGenerateCssFile");
            target.Less.ShowPreviewPane = GetBoolean("LessShowPreviewWindow");
            target.Less.Minify = GetBoolean("LessMinify");
            target.Less.CompileOnBuild = GetBoolean("LessCompileOnBuild");
            target.Less.GenerateSourceMaps = GetBoolean("LessSourceMaps");
            target.Less.OutputDirectory = GetString("LessCompileToLocation");

            // SASS
            target.Sass.CompileOnSave = GetBoolean("SassGenerateCssFile");
            target.Sass.ShowPreviewPane = GetBoolean("SassShowPreviewWindow");
            target.Sass.Minify = GetBoolean("SassMinify");
            target.Sass.CompileOnBuild = GetBoolean("SassCompileOnBuild");
            target.Sass.GenerateSourceMaps = GetBoolean("SassSourceMaps");
            target.Sass.OutputDirectory = GetString("SassCompileToLocation");

            // TypeScript
            target.TypeScript.ShowPreviewPane = GetBoolean("TypeScriptShowPreviewWindow");
            target.TypeScript.ShowPreviewPane = GetBoolean("TypeScriptBraceCompletion");

            // CoffeeScript
            target.CoffeeScript.CompileOnSave = GetBoolean("CoffeeScriptGenerateJsFile");
            target.CoffeeScript.ShowPreviewPane = GetBoolean("CoffeeScriptShowPreviewWindow");
            target.CoffeeScript.Minify = GetBoolean("CoffeeScriptMinify");
            target.CoffeeScript.WrapClosure = GetBoolean("CoffeeScriptWrapClosure");
            target.CoffeeScript.CompileOnBuild = GetBoolean("CoffeeScriptCompileOnBuild");
            target.CoffeeScript.GenerateSourceMaps = GetBoolean("CoffeeScriptSourceMaps");
            target.CoffeeScript.OutputDirectory = GetString("CoffeeScriptCompileToLocation");

            // Markdown
            target.Markdown.ShowPreviewPane = GetBoolean("MarkdownShowPreviewWindow");
            target.Markdown.CompileOnSave = GetBoolean("MarkdownEnableCompiler");
            target.Markdown.OutputDirectory = GetString("MarkdownCompileToLocation");

            target.Markdown.AutoHyperlinks = GetBoolean("MarkdownAutoHyperlinks");
            target.Markdown.LinkEmails = GetBoolean("MarkdownLinkEmails");
            target.Markdown.AutoNewLines = GetBoolean("MarkdownAutoNewLine");
            target.Markdown.GenerateXHTML = GetBoolean("MarkdownGenerateXHTML");
            target.Markdown.EncodeProblemUrlCharacters = GetBoolean("MarkdownEncodeProblemUrlCharacters");
            target.Markdown.StrictBoldItalic = GetBoolean("MarkdownStrictBoldItalic");

            // SVG
            target.General.SvgPreviewPane = GetBoolean("SvgShowPreviewWindow");

            // CSS
            target.Css.ValidateStarSelector = GetBoolean("CssValidateStarSelector");
            target.Css.ValidateOverQualifiedSelector = GetBoolean("CSSValidateOverQualifiedSelector");
            target.Css.ValidationLocation = (WarningLocation)GetInt("CssErrorLocation");
            target.Css.ValidateEmbedImages = GetBoolean("CssValidateEmbedImages");
            target.Css.ShowBrowserTooltip = GetBoolean("CssShowBrowserTooltip");
            target.Css.SyncVendorValues = GetBoolean("CssSyncVendorValues");
            target.Css.ShowInitialInherit = GetBoolean("CssShowInitialInherit");
            target.Css.ShowUnsupported = GetBoolean("CssShowUnsupported");
            target.Css.MinifyOnSave = GetBoolean("CssEnableMinification");
            target.Css.ValidateZeroUnit = GetBoolean("CssValidateZeroUnit");
            target.Css.ValidateVendorSpecifics = GetBoolean("ValidateVendorSpecifics");
            target.Css.GzipMinifiedFiles = GetBoolean("CssEnableGzipping");
            target.Css.AdjustRelativePaths = !GetBoolean("CssPreserveRelativePathsOnMinify");

            // JavaScript
            target.JavaScript.MinifyOnSave = GetBoolean("JavaScriptEnableMinification");
            target.JavaScript.GenerateSourceMaps = GetBoolean("JavaScriptGenerateSourceMaps");
            target.JavaScript.GzipMinifiedFiles = GetBoolean("JavaScriptEnableGzipping");
            target.JavaScript.BlockCommentCompletion = GetBoolean("JavaScriptCommentCompletion");

            // JSHint
            target.JavaScript.LintOnSave = GetBoolean("JsHintEnable");
            target.JavaScript.LintOnBuild = GetBoolean("JsHintRunOnBuild");
            target.JavaScript.LintResultLocation = (ErrorLocation)GetInt("JsHintErrorLocation");

            // TSLint
            target.TypeScript.LintOnSave = GetBoolean("TsLintEnable");
            target.TypeScript.LintOnBuild = GetBoolean("TsLintRunOnBuild");
            target.TypeScript.LintResultLocation = (ErrorLocation)GetInt("TsLintErrorLocation");

            // Browser Link
            target.BrowserLink.IgnorePatterns = GetString("UnusedCss_IgnorePatterns");
            target.BrowserLink.EnableBrowserLinkMenu = GetBoolean("EnableBrowserLinkMenu");

            //Pixel Pushing mode
            target.BrowserLink.EnablePixelPushing = GetBoolean("PixelPushing_OnByDefault");

            //target. = GetBoolean("BrowserLink_ShowMenu");

        }

        bool GetBoolean(string propertyName)
        {
            return (bool?)settingsElement.Element(propertyName) ?? false;
        }

        int GetInt(string propertyName)
        {
            return (int?)settingsElement.Element(propertyName) ?? -1;
        }

        string GetString(string propertyName)
        {
            return (string)settingsElement.Element(propertyName);
        }
    }
}