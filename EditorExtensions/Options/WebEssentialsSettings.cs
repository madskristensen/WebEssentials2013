
namespace MadsKristensen.EditorExtensions
{
    static class WESettings
    {
        public static class Keys
        {
            // General
            public const string EnableHtmlZenCoding = "HtmlEnableZenCoding";
            public const string EnableMustache = "HtmlEnableMustache";
            public const string KeepImportantComments = "KeepImportantComments";

            // LESS
            public const string GenerateCssFileFromLess = "LessGenerateCssFile";
            public const string ShowLessPreviewWindow = "LessShowPreviewWindow";
            public const string LessMinify = "LessMinify";
            public const string LessCompileOnBuild = "LessCompileOnBuild";
            public const string LessCompileToFolder = "LessCompileToFolder";

            // SCSS
            public const string GenerateCssFileFromScss = "ScssGenerateCssFile";
            public const string ShowScssPreviewWindow = "ScssShowPreviewWindow";
            public const string ScssMinify = "ScssMinify";
            public const string ScssCompileOnBuild = "ScssCompileOnBuild";
            public const string ScssCompileToFolder = "ScssCompileToFolder";

            // CoffeeScript
            public const string GenerateJsFileFromCoffeeScript = "CoffeeScriptGenerateJsFile";
            public const string ShowCoffeeScriptPreviewWindow = "CoffeeScriptShowPreviewWindow";
            public const string CoffeeScriptMinify = "CoffeeScriptMinify";
            public const string WrapCoffeeScriptClosure = "CoffeeScriptWrapClosure";
            public const string CoffeeScriptCompileToFolder = "CoffeeScriptCompileToFolder";
            public const string CoffeeScriptCompileOnBuild = "CoffeeScriptCompileOnBuild";

            // CSS
            public const string ValidateStarSelector = "CssValidateStarSelector";
            public const string ValidateOverQualifiedSelector = "CSSValidateOverQualifiedSelector";
            public const string CssErrorLocation = "CssErrorLocation";
            public const string EnableCssSelectorHighligting = "CssEnableSelectorHighligting";
            public const string ValidateEmbedImages = "CssValidateEmbedImages";
            public const string ShowBrowserTooltip = "CssShowBrowserTooltip";
            public const string SyncVendorValues = "CssSyncVendorValues";
            public const string ShowInitialInherit = "CssShowInitialInherit";
            public const string ShowUnsupported = "CssShowUnsupported";
            public const string EnableCssMinification = "CssEnableMinification";
            public const string ValidateZeroUnit = "CssValidateZeroUnit";
            public const string ValidateVendorSpecifics = "ValidateVendorSpecifics";
            public const string EnableSpeedTyping = "EnableSpeedTyping";
            public const string CssEnableGzipping = "CssEnableGzipping";

            // JavaScript
            public const string EnableJavascriptRegions = "JavascriptEnableRegions";
            public const string EnableJsMinification = "JavaScriptEnableMinification";
            public const string JavaScriptAutoCloseBraces = "JavaScriptAutoCloseBraces";
            public const string JavaScriptOutlining = "JavaScriptOutlining";
            public const string GenerateJavaScriptSourceMaps = "JavaScriptGenerateSourceMaps";
            public const string JavaScriptEnableGzipping = "JavaScriptEnableGzipping";

            // TypeScript
            //public const string GenerateJsFileFromTypeScript = "TypeScriptGenerateJsFile";
            //public const string ShowTypeScriptPreviewWindow = "TypeScriptShowPreviewWindow";
            //public const string CompileTypeScriptOnBuild = "TypeScriptCompileOnBuild";
            //public const string TypeScriptKeepComments = "TypeScriptKeepComments";
            //public const string TypeScriptUseAmdModule = "TypeScriptUseAmdModule";
            //public const string TypeScriptCompileES3 = "TypeScriptCompileES3";
            //public const string TypeScriptProduceSourceMap = "TypeScriptProduceSourceMap";
            //public const string TypeScriptMinify = "TypeScriptMinify";
            //public const string TypeScriptAddGeneratedFilesToProject = "TypeScriptAddGeneratedFilesToProject";
            //public const string TypeScriptResaveWithUtf8BOM = "TypeScriptResaveWithUtf8BOM";

            // JSHint
            public const string EnableJsHint = "JsHintEnable";
            public const string JsHint_ignoreFiles = "JsHint_ignoreFiles";
            public const string RunJsHintOnBuild = "JsHintRunOnBuild";
            public const string JsHintErrorLocation = "JsHintErrorLocation";
            public const string JsHint_eqeqeq = "JsHint_eqeqeq";
            public const string JsHint_bitwise = "JsHint_bitwise";
            public const string JsHint_maxerr = "JsHint_maxerr"; // int
            public const string JsHint_camelcase = "JsHint_camelcase";
            public const string JsHint_curly = "JsHint_curly";
            public const string JsHint_forin = "JsHint_forin";
            public const string JsHint_immed = "JsHint_immed";
            public const string JsHint_indent = "JsHint_indent"; // int
            public const string JsHint_latedef = "JsHint_latedef";
            public const string JsHint_newcap = "JsHint_newcap";
            public const string JsHint_noarg = "JsHint_noarg";
            public const string JsHint_noempty = "JsHint_noempty";
            public const string JsHint_nonew = "JsHint_nonew";
            public const string JsHint_plusplus = "JsHint_plusplus";
            public const string JsHint_quotmark = "JsHint_quotmark";
            public const string JsHint_regexp = "JsHint_regexp";
            public const string JsHint_undef = "JsHint_undef";
            public const string JsHint_unused = "JsHint_unused";
            public const string JsHint_strict = "JsHint_strict";
            public const string JsHint_trailing = "JsHint_trailing";

            // Relaxing
            public const string JsHint_asi = "JsHint_asi";
            public const string JsHint_boss = "JsHint_boss";
            public const string JsHint_debug = "JsHint_debug";
            public const string JsHint_eqnull = "JsHint_eqnull";
            public const string JsHint_esnext = "JsHint_esnext";
            public const string JsHint_evil = "JsHint_evil";
            public const string JsHint_expr = "JsHint_expr";
            public const string JsHint_funcscope = "JsHint_funcscope";
            public const string JsHint_globalstrict = "JsHint_globalstrict";
            public const string JsHint_iterator = "JsHint_iterator";
            public const string JsHint_lastsemic = "JsHint_lastsemic";
            public const string JsHint_laxbreak = "JsHint_laxbreak";
            public const string JsHint_laxcomma = "JsHint_laxcomma";
            public const string JsHint_loopfunc = "JsHint_loopfunc";
            public const string JsHint_multistr = "JsHint_multistr";
            public const string JsHint_onecase = "JsHint_onecase";
            public const string JsHint_proto = "JsHint_proto";
            public const string JsHint_regexdash = "JsHint_regexdash";
            public const string JsHint_scripturl = "JsHint_scripturl";
            public const string JsHint_smarttabs = "JsHint_smarttabs";
            public const string JsHint_shadow = "JsHint_shadow";
            public const string JsHint_sub = "JsHint_sub";
            public const string JsHint_supernew = "JsHint_supernew";
            public const string JsHint_validthis = "JsHint_validthis";

            // Environment
            public const string JsHint_browser = "JsHint_browser";
            public const string JsHint_devel = "JsHint_devel";
            public const string JsHint_jquery = "JsHint_jquery";
            public const string JsHint_couch = "JsHint_couch";
            public const string JsHint_dojo = "JsHint_dojo";
            public const string JsHint_mootools = "JsHint_mootools";
            public const string JsHint_node = "JsHint_node";
            public const string JsHint_nonstandard = "JsHint_nonstandard";
            public const string JsHint_prototypejs = "JsHint_prototypejs";
            public const string JsHint_rhino = "JsHint_rhino";
            public const string JsHint_worker = "JsHint_worker";
            public const string JsHint_wsh = "JsHint_wsh";

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

        private static Settings _projectStore;

        static WESettings()
        {
            _projectStore = new Settings();
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
