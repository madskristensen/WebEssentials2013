
using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.Settings
{
    ///<summary>Migrates settings from legacy XML settings files to the new ConfOxide-based settings objects.</summary>
    public class SettingsMigrator
    {
        readonly XElement settingsElement;
        public SettingsMigrator(string sourcePath) : this(XDocument.Load(sourcePath)) { }
        public SettingsMigrator(XDocument source)
        {
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
            target.Html.AutoMinify = GetBoolean("HtmlEnableMinification");

            // LESS
            target.Less.CompileOnSave = GetBoolean("LessGenerateCssFile");
            target.Less.ShowPreviewPane = GetBoolean("LessShowPreviewWindow");
            target.Less.CompileOnBuild = GetBoolean("LessCompileOnBuild");
            target.Less.GenerateSourceMaps = GetBoolean("LessSourceMaps");
            target.Less.OutputDirectory = GetNonBooleanString("LessCompileToLocation");

            // SASS
            target.Scss.CompileOnSave = GetBoolean("SassGenerateCssFile");
            target.Scss.ShowPreviewPane = GetBoolean("SassShowPreviewWindow");
            target.Scss.CompileOnBuild = GetBoolean("SassCompileOnBuild");
            target.Scss.GenerateSourceMaps = GetBoolean("SassSourceMaps");
            target.Scss.OutputDirectory = GetNonBooleanString("SassCompileToLocation");

            // TypeScript
            target.TypeScript.ShowPreviewPane = GetBoolean("TypeScriptShowPreviewWindow");

            // CoffeeScript
            target.CoffeeScript.CompileOnSave = GetBoolean("CoffeeScriptGenerateJsFile");
            target.CoffeeScript.ShowPreviewPane = GetBoolean("CoffeeScriptShowPreviewWindow");
            target.CoffeeScript.WrapClosure = GetBoolean("CoffeeScriptWrapClosure");
            target.CoffeeScript.CompileOnBuild = GetBoolean("CoffeeScriptCompileOnBuild");
            target.CoffeeScript.GenerateSourceMaps = GetBoolean("CoffeeScriptSourceMaps");
            target.CoffeeScript.OutputDirectory = GetNonBooleanString("CoffeeScriptCompileToLocation");

            // Markdown
            target.Markdown.ShowPreviewPane = GetBoolean("MarkdownShowPreviewWindow");
            target.Markdown.CompileOnSave = GetBoolean("MarkdownEnableCompiler");
            target.Markdown.OutputDirectory = GetNonBooleanString("MarkdownCompileToLocation");

            target.Markdown.AutoHyperlink = GetBoolean("MarkdownAutoHyperlinks");
            target.Markdown.LinkEmails = GetBoolean("MarkdownLinkEmails");
            target.Markdown.AutoNewLines = GetBoolean("MarkdownAutoNewLine");
            target.Markdown.GenerateXHTML = GetBoolean("MarkdownGenerateXHTML");
            target.Markdown.EncodeProblemUrlCharacters = GetBoolean("MarkdownEncodeProblemUrlCharacters");
            target.Markdown.StrictBoldItalic = GetBoolean("MarkdownStrictBoldItalic");

            // SVG
            target.General.SvgPreviewPane = GetBoolean("SvgShowPreviewWindow");

            // CSS
            target.Css.ValidateStarSelector = GetBoolean("CssValidateStarSelector");
            target.Css.ValidationLocation = (WarningLocation)GetInt("CssErrorLocation");
            target.Css.ValidateEmbedImages = GetBoolean("CssValidateEmbedImages");
            target.Css.ShowBrowserTooltip = GetBoolean("CssShowBrowserTooltip");
            target.Css.SyncVendorValues = GetBoolean("CssSyncVendorValues");
            target.Css.ShowInitialInherit = GetBoolean("CssShowInitialInherit");
            target.Css.ShowUnsupported = GetBoolean("CssShowUnsupported");
            target.Css.AutoMinify = GetBoolean("CssEnableMinification");
            target.Css.ValidateZeroUnit = GetBoolean("CssValidateZeroUnit");
            target.Css.ValidateVendorSpecifics = GetBoolean("ValidateVendorSpecifics");
            target.Css.GzipMinifiedFiles = GetBoolean("CssEnableGzipping");
            target.Css.AdjustRelativePaths = !GetBoolean("CssPreserveRelativePathsOnMinify");

            // JavaScript
            target.JavaScript.AutoMinify = GetBoolean("JavaScriptEnableMinification");
            target.JavaScript.GenerateSourceMaps = GetBoolean("JavaScriptGenerateSourceMaps");
            target.JavaScript.GzipMinifiedFiles = GetBoolean("JavaScriptEnableGzipping");
            target.JavaScript.BlockCommentCompletion = GetBoolean("JavaScriptCommentCompletion");

            // JSHint
            target.JavaScript.LintOnSave = GetBoolean("JsHintEnable");
            target.JavaScript.LintOnBuild = GetBoolean("JsHintRunOnBuild");
            target.JavaScript.LintResultLocation = GetEnum<TaskErrorCategory>("JsHintErrorLocation") ?? target.JavaScript.LintResultLocation;

            // TSLint
            target.TypeScript.LintOnSave = GetBoolean("TsLintEnable");
            target.TypeScript.LintOnBuild = GetBoolean("TsLintRunOnBuild");
            target.TypeScript.LintResultLocation = GetEnum<TaskErrorCategory>("TsLintErrorLocation") ?? target.TypeScript.LintResultLocation;

            // Browser Link
            target.BrowserLink.CssIgnorePatterns = GetString("UnusedCss_IgnorePatterns");
            target.BrowserLink.EnableMenu = GetBoolean("EnableBrowserLinkMenu");

            //Pixel Pushing mode
            target.BrowserLink.EnablePixelPushing = GetBoolean("PixelPushing_OnByDefault");

            target.BrowserLink.ShowMenu = GetBoolean("BrowserLink_ShowMenu");

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

        // Use this for legacy settings that were changed from booleans and may have bad values
        string GetNonBooleanString(string propertyName)
        {
            var value = GetString(propertyName);
            bool unused;
            if (bool.TryParse(value, out unused))
                return null;
            return value;
        }

        T? GetEnum<T>(string key) where T : struct
        {
            var retVal = Enum.ToObject(typeof(T), GetInt(key));
            if (!Enum.IsDefined(typeof(T), retVal))
                return null;
            return (T?)retVal;
        }
    }
}