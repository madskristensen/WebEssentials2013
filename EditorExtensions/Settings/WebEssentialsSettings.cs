
namespace MadsKristensen.EditorExtensions
{
    static class WESettings
    {
        public static class Keys
        {
            public const string JavaScriptCamelCasePropertyNames = "JavaScriptCamelCasePropertyNames";
            public const string JavaScriptCamelCaseClassNames = "JavaScriptCamelCaseClassNames";
            // General
            public const string KeepImportantComments = "KeepImportantComments";
            public const string AllMessagesToOutputWindow = "AllMessagesToOutputWindow";

            // HTML
            public const string EnableEnterFormat = "EnableEnterFormat";
            public const string EnableAngularValidation = "EnableAngularValidation";
            public const string EnableHtmlMinification = "HtmlEnableMinification";

            // LESS
            public const string GenerateCssFileFromLess = "LessGenerateCssFile";
            public const string ShowLessPreviewWindow = "LessShowPreviewWindow";
            public const string LessMinify = "LessMinify";
            public const string LessCompileOnBuild = "LessCompileOnBuild";
            public const string LessSourceMaps = "LessSourceMaps";
            public const string LessCompileToLocation = "LessCompileToLocation";

            // SASS
            public const string GenerateCssFileFromSass = "SassGenerateCssFile";
            public const string ShowSassPreviewWindow = "SassShowPreviewWindow";
            public const string SassMinify = "SassMinify";
            public const string SassCompileOnBuild = "SassCompileOnBuild";
            public const string SassSourceMaps = "SassSourceMaps";
            public const string SassCompileToLocation = "SassCompileToLocation";

            // TypeScript
            public const string ShowTypeScriptPreviewWindow = "TypeScriptShowPreviewWindow";
            public const string TypeScriptBraceCompletion = "TypeScriptBraceCompletion";

            // CoffeeScript
            public const string GenerateJsFileFromCoffeeScript = "CoffeeScriptGenerateJsFile";
            public const string ShowCoffeeScriptPreviewWindow = "CoffeeScriptShowPreviewWindow";
            public const string CoffeeScriptMinify = "CoffeeScriptMinify";
            public const string WrapCoffeeScriptClosure = "CoffeeScriptWrapClosure";
            public const string CoffeeScriptCompileOnBuild = "CoffeeScriptCompileOnBuild";
            public const string CoffeeScriptSourceMaps = "CoffeeScriptSourceMaps";
            public const string CoffeeScriptCompileToLocation = "CoffeeScriptCompileToLocation";

            // Markdown
            public const string MarkdownShowPreviewWindow = "MarkdownShowPreviewWindow";
            public const string MarkdownEnableCompiler = "MarkdownEnableCompiler";
            public const string MarkdownCompileToLocation = "MarkdownCompileToLocation";

            public const string MarkdownAutoHyperlinks = "MarkdownAutoHyperlinks";
            public const string MarkdownLinkEmails = "MarkdownLinkEmails";
            public const string MarkdownAutoNewLine = "MarkdownAutoNewLine";
            public const string MarkdownGenerateXHTML = "MarkdownGenerateXHTML";
            public const string MarkdownEncodeProblemUrlCharacters = "MarkdownEncodeProblemUrlCharacters";
            public const string MarkdownStrictBoldItalic = "MarkdownStrictBoldItalic";

            // SVG
            public const string SvgShowPreviewWindow = "SvgShowPreviewWindow";

            // CSS
            public const string ValidateStarSelector = "CssValidateStarSelector";
            public const string ValidateOverQualifiedSelector = "CSSValidateOverQualifiedSelector";
            public const string CssErrorLocation = "CssErrorLocation";
            public const string ValidateEmbedImages = "CssValidateEmbedImages";
            public const string ShowBrowserTooltip = "CssShowBrowserTooltip";
            public const string SyncVendorValues = "CssSyncVendorValues";
            public const string ShowInitialInherit = "CssShowInitialInherit";
            public const string ShowUnsupported = "CssShowUnsupported";
            public const string EnableCssMinification = "CssEnableMinification";
            public const string ValidateZeroUnit = "CssValidateZeroUnit";
            public const string ValidateVendorSpecifics = "ValidateVendorSpecifics";
            public const string CssEnableGzipping = "CssEnableGzipping";
            public const string CssPreserveRelativePathsOnMinify = "CssPreserveRelativePathsOnMinify";

            // JavaScript
            public const string EnableJsMinification = "JavaScriptEnableMinification";
            public const string GenerateJavaScriptSourceMaps = "JavaScriptGenerateSourceMaps";
            public const string JavaScriptEnableGzipping = "JavaScriptEnableGzipping";

            // JSHint
            public const string EnableJsHint = "JsHintEnable";
            public const string RunJsHintOnBuild = "JsHintRunOnBuild";
            public const string JsHintErrorLocation = "JsHintErrorLocation";
            
            // Browser Link
            public const string UnusedCss_IgnorePatterns = "UnusedCss_IgnorePatterns";
            public const string EnableBrowserLinkMenu = "EnableBrowserLinkMenu";

            //Pixel Pushing mode
            public const string PixelPushing_OnByDefault = "PixelPushing_OnByDefault";

            public const string BrowserLink_ShowMenu = "BrowserLink_ShowMenu";

            public enum ErrorLocation
            {
                Warnings = 0,
                Messages = 1,
            }

            public enum FullErrorLocation
            {
                Errors = 0,
                Warnings = 1,
                Messages = 2,
            }
        }

        public static bool GetBoolean(string propertyName)
        {
            bool result;
            object value = Settings.GetValue(propertyName);

            if (value != null && bool.TryParse(value.ToString(), out result))
            {
                return result;
            }

            return false;
        }

        public static int GetInt(string propertyName)
        {
            int result;
            object value = Settings.GetValue(propertyName);

            if (value != null && int.TryParse(value.ToString(), out result))
            {
                return result;
            }

            return -1;
        }

        public static string GetString(string propertyName)
        {
            object value = Settings.GetValue(propertyName);

            if (value != null)
            {
                return value.ToString();
            }

            return null;
        }
    }
}