using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ConfOxide;
using MarkdownSharp;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.Settings
{
    public sealed class WESettings : SettingsBase<WESettings>
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly WESettings Instance = new WESettings();

        public GeneralSettings General { get; private set; }
        public CodeGenSettings CodeGen { get; private set; }
        public BrowserLinkSettings BrowserLink { get; private set; }
        public SpriteSettings Sprite { get; private set; }

        // The names of these properties must match VS ContentTypes
        public TypeScriptSettings TypeScript { get; private set; }

        public CssSettings Css { get; private set; }
        public HtmlSettings Html { get; private set; }
        public JavaScriptSettings JavaScript { get; private set; }

        public LessSettings Less { get; private set; }
        public ScssSettings Scss { get; private set; }
        public CoffeeScriptSettings CoffeeScript { get; private set; }
        public LiveScriptSettings LiveScript { get; private set; }
        public MarkdownSettings Markdown { get; private set; }
        public SweetJsSettings SweetJs { get; private set; }
    }

    public sealed class SpriteSettings : SettingsBase<SpriteSettings>
    {
        [Category("Sprite")]
        [DisplayName("Optmize sprite files")]
        [Description("Optmize sprite using image optmizer. Set it to false, if you need maximum resolution.")]
        [DefaultValue(true)]
        public bool Optimize { get; set; }

        [Category("Sprite")]
        [DisplayName("Is Vertical")]
        [Description("Sprite image would be generated vertically. Set it to false, to generate it horizontally.")]
        [DefaultValue(true)]
        public bool IsVertical { get; set; }

        [Category("Sprite")]
        [DisplayName("Run on build")]
        [Description("Regenerate the sprite when building the solution.")]
        [DefaultValue(false)]
        public bool RunOnBuild { get; set; }

        [Category("Sprite")]
        [DisplayName("Use full path for naming identifiers")]
        [Description("Use full path to generate unique class or mixin name in CSS, LESS and SASS files. Consider disabling this if you want class names to be filename only.")]
        [DefaultValue(true)]
        public bool UseFullPathForIdentifierName { get; set; }

        [Category("Sprite")]
        [DisplayName("Use absolute URL path in generated CSS-like files")]
        [Description("Use absolute path in the generated CSS-like files. By default, the URLs are relative to sprite image file (and the location of CSS, LESS and SCSS).")]
        [DefaultValue(false)]
        public bool UseAbsoluteUrl { get; set; }

        [Category("Sprite")]
        [DisplayName("Custom CSS output directory")]
        [Description("Specifies a custom subfolder to save CSS files to. By default, compiled output will be placed in the same folder and nested under the original file.")]
        [DefaultValue(null)]
        public string CssOutputDirectory { get; set; }

        [Category("Sprite")]
        [DisplayName("Custom LESS output directory")]
        [Description("Specifies a custom subfolder to save LESS files to. By default, compiled output will be placed in the same folder and nested under the original file.")]
        [DefaultValue(null)]
        public string LessOutputDirectory { get; set; }

        [Category("Sprite")]
        [DisplayName("Custom SCSS output directory")]
        [Description("Specifies a custom subfolder to save SCSS files to. By default, compiled output will be placed in the same folder and nested under the original file.")]
        [DefaultValue(null)]
        public string ScssOutputDirectory { get; set; }
    }

    public sealed class GeneralSettings : SettingsBase<GeneralSettings>, IMarginSettings
    {
        [Category("Minification")]
        [DisplayName("Keep important comments")]
        [Description("Preserve important comments (/*! ... */) when minifying JS and CSS files.")]
        [DefaultValue(true)]
        public bool KeepImportantComments { get; set; }

        [Category("IDE")]
        [DisplayName("Redirect messages to Output Window")]
        [Description("Show user errors in the Output Window instead of showing message boxes.")]
        [DefaultValue(false)]
        public bool AllMessagesToOutputWindow { get; set; }

        //[Category("CSS")]
        //[DisplayName("Chain Compilation")]
        //[Description("Compile the dependents chain when a LESS or SASS file is saved.")]
        //[DefaultValue(false)]
        //public bool ChainCompilation { get; set; }

        [Category("SVG Files")]
        [DisplayName("Show preview pane")]
        [Description("Show a preview pane when editing SVG files.")]
        [DefaultValue(true)]
        public bool SvgPreviewPane { get; set; }

        bool IMarginSettings.ShowPreviewPane { get { return SvgPreviewPane; } }
    }

    public sealed class BrowserLinkSettings : SettingsBase<BrowserLinkSettings>
    {
        [Category("Browser Link")]
        [DisplayName("Enable Browser Link menu")]
        [Description("Enable the menu that shows up in the browser. Requires restart.")]
        [DefaultValue(true)]
        public bool EnableMenu { get; set; }

        [Category("CSS")]
        [DisplayName("CSS usage files to ignore")]
        [Description("A semicolon-separated list of file patterns to ignore.")]
        [DefaultValue("bootstrap*; reset.css; normalize.css; jquery*; toastr*; foundation*; animate*; inuit*; elements*; ratchet*; hint*; flat-ui*; 960*; skeleton*")]
        public string CssIgnorePatterns { get; set; }  // TODO: Switch to List<string> & check property designer support

        [Category("CSS")]
        [DisplayName("Enable f12 auto-sync")]
        [Description("Automatically synchronize changes made in the browser dev tools with CSS files in Visual Studio. If this is turned off, you can synchronize changes explicitly in the Browser Link menu.")]
        [DefaultValue(true)]
        public bool EnablePixelPushing { get; set; }

        [Browsable(false)]
        [DefaultValue(true)]
        public bool ShowMenu { get; set; }
    }

    public sealed class CodeGenSettings : SettingsBase<CodeGenSettings>
    {
        [DisplayName("Use LowerCamelCase for property names")]
        [Description("Use LowerCamelCase instead of lowerCamelCase for property names in generated JS/TS files.")]
        [DefaultValue(true)]
        public bool CamelCasePropertyNames { get; set; }

        [DisplayName("Use LowerCamelCase for type names")]
        [Description("Use LowerCamelCase instead of lowerCamelCase for type names in generated JS/TS files.")]
        [DefaultValue(false)]
        public bool CamelCaseTypeNames { get; set; }

        [DisplayName("Use LowerCamelCase for enumeration values")]
        [Description("Use LowerCamelCase for enumeration values in generated JS/TS files.")]
        [DefaultValue(false)]
        public bool CamelCaseEnumerationValues { get; set; }

        [DisplayName("Add TypeScript reference")]
        [Description("Add TypeScript reference path in generated JS/TS files.")]
        [DefaultValue(true)]
        public bool AddTypeScriptReferencePath { get; set; }
    }

    public interface ILinterSettings
    {
        bool LintOnSave { get; }
        bool LintOnBuild { get; }
        TaskErrorCategory LintResultLocation { get; }
    }

    public class LinterSettings<T> : SettingsBase<T>, ILinterSettings where T : LinterSettings<T>
    {
        [Category("Linter")]
        [DisplayName("Run on save")]
        [Description("Run linter when saving each source file.")]
        [DefaultValue(true)]
        public bool LintOnSave { get; set; }

        [Category("Linter")]
        [DisplayName("Run on build")]
        [Description("Lint all files when building the solution.")]
        [DefaultValue(false)]
        public bool LintOnBuild { get; set; }

        [Category("Linter")]
        [DisplayName("Results location")]
        [Description("Where to show messages from the linter.")]
        [DefaultValue(TaskErrorCategory.Message)]
        public TaskErrorCategory LintResultLocation { get; set; }
    }

    public sealed class TypeScriptSettings : LinterSettings<TypeScriptSettings>, IMarginSettings
    {
        [Category("Editor")]
        [DisplayName("Show preview pane")]
        [Description("Show a preview pane containing the generated JavaScript when editing TypeScript files.")]
        [DefaultValue(true)]
        public bool ShowPreviewPane { get; set; }
    }

    public sealed class HtmlSettings : SettingsBase<HtmlSettings>, IMinifierSettings, IBundleSettings
    {
        [DisplayName("Auto-format HTML on Enter")]
        [Description("Automatically format HTML source when pressing Enter.")]
        [DefaultValue(true)]
        public bool EnableEnterFormat { get; set; }

        [DisplayName("Minify files on save")]
        [Description("Update any .min.html file when saving the corresponding .html file. To create a .min.html file, right-click a .html file.")]
        [DefaultValue(false)]
        public bool AutoMinify { get; set; }

        [Category("Minification")]
        [DisplayName("Create gzipped files")]
        [Description("Also save separate gzipped files when minifying. This option has no effect when Minify on save is disabled.")]
        [DefaultValue(false)]
        public bool GzipMinifiedFiles { get; set; }

        [DisplayName("Enable Angular.js validation")]
        [Description("Validate HTML files against Angular.js best practices.")]
        [DefaultValue(true)]
        public bool EnableAngularValidation { get; set; }

        [DisplayName("Enable Bootstrap validation")]
        [Description("Validate HTML files against Bootstrap best practices.")]
        [DefaultValue(true)]
        public bool EnableBootstrapValidation { get; set; }

        [DisplayName("Enable Zurb Foundation validation")]
        [Description("Validate HTML files against Foundation best practices.")]
        [DefaultValue(true)]
        public bool EnableFoundationValidation { get; set; }

        [Browsable(false)]
        public ObservableCollection<ImageDropFormat> ImageDropFormats { get; private set; }
        protected override void ResetCustom()
        {
            ImageDropFormats.Add(new ImageDropFormat("Simple Image Tag", @"<img src=""{0}"" alt="""" />"));
            ImageDropFormats.Add(new ImageDropFormat("Enclosed in Div", @"<div><img src=""{0}"" alt="""" /></div>"));
            ImageDropFormats.Add(new ImageDropFormat("Enclosed as List Item", @"<li id=""item_{1}""><img src=""{0}"" alt="""" /></li>"));
            ImageDropFormats.Add(new ImageDropFormat("Inline CSS", @"<div style=""background-image=url('{0}')""></div>"));
        }

        #region Bundle Settings.

        [Category("Bundle")]
        [DisplayName("Minify files on making bundle")]
        [Description("Make minified version of the file when bundle is generated. The minified file would be updated if the auto-minfied option is enabled.")]
        [DefaultValue(false)]
        public bool MakeMinified { get; set; }

        [Category("Bundle")]
        [DisplayName("Run on build")]
        [Description("Regenerate the sprite when building the solution.")]
        [DefaultValue(false)]
        public bool RunOnBuild { get; set; }

        [Category("Bundle")]
        [DisplayName("Custom HTML output directory")]
        [Description("Specifies a custom subfolder to save HTML files to. By default, compiled output will be placed in the same folder and nested under the original file.")]
        [DefaultValue(null)]
        public string OutputDirectory { get; set; }

        #endregion
    }

    public sealed class ImageDropFormat : SettingsBase<ImageDropFormat>
    {
        public ImageDropFormat() { }
        public ImageDropFormat(string name, string htmlFormat)
        {
            Name = name;
            HtmlFormat = htmlFormat;
        }
        public string Name { get; set; }
        public string HtmlFormat { get; set; }
    }

    public sealed class JavaScriptSettings : LinterSettings<JavaScriptSettings>, IMinifierSettings, ISourceMapSettings, IBundleSettings
    {
        #region Minification
        [Category("Minification")]
        [DisplayName("Minify files on save")]
        [Description("Update any .min.js file when saving the corresponding .js file. To create a .min.js file, right-click a .js file.")]
        [DefaultValue(false)]
        public bool AutoMinify { get; set; }

        [Category("Minification")]
        [DisplayName("Create gzipped files")]
        [Description("Also save separate gzipped files when minifying. This option has no effect when Minify on save is disabled.")]
        [DefaultValue(false)]
        public bool GzipMinifiedFiles { get; set; }

        [Category("Minification")]
        [DisplayName("Create source map files")]
        [Description("Generate source map files when minifying or bundling.")]
        [DefaultValue(true)]
        public bool GenerateSourceMaps { get; set; }
        #endregion

        [Category("Editor")]
        [DisplayName("Auto-complete multi-line comments")]
        [Description("Auto-complete /* */ comment blocks, and insert * on new lines.")]
        [DefaultValue(true)]
        public bool BlockCommentCompletion { get; set; }

        #region Bundle Settings.

        [Category("Bundle")]
        [DisplayName("Minify files on making bundle")]
        [Description("Make minified version of the file when bundle is generated. The minified file would be updated if the auto-minfied option is enabled.")]
        [DefaultValue(false)]
        public bool MakeMinified { get; set; }

        [Category("Bundle")]
        [DisplayName("Run on build")]
        [Description("Regenerate the sprite when building the solution.")]
        [DefaultValue(false)]
        public bool RunOnBuild { get; set; }

        [Category("Bundle")]
        [DisplayName("Custom JavaScript output directory")]
        [Description("Specifies a custom subfolder to save JavaScript files to. By default, compiled output will be placed in the same folder and nested under the original file.")]
        [DefaultValue(null)]
        public string OutputDirectory { get; set; }

        #endregion
    }

    public sealed class CssSettings : SettingsBase<CssSettings>, IMinifierSettings, IBundleSettings
    {
        #region Minification
        [Category("Minification")]
        [DisplayName("Minify files on save")]
        [Description("Update any .min.css file when saving the corresponding .css file. To create a .min.css file, right-click a .css file. This also applies to compiled LESS and SASS files.")]
        [DefaultValue(false)]
        public bool AutoMinify { get; set; }

        [Category("Minification")]
        [DisplayName("Create gzipped files")]
        [Description("Also save separate gzipped files when minifying or bundling.")]
        [DefaultValue(false)]
        public bool GzipMinifiedFiles { get; set; }
        #endregion

        #region Warnings
        [Category("Performance Warnings")]
        [DisplayName("Results location")]
        [Description("Where to display performance warnings. To use the Errors category, select Warnings, then disable Show errors as warnings in CSS Advanced Options.")]
        [DefaultValue(true)]
        public WarningLocation ValidationLocation { get; set; }
        [Category("Performance Warnings")]
        [DisplayName("Disallow universal selector")]
        [Description("Warn on selectors that contain the universal selector (*).")]
        [DefaultValue(true)]
        public bool ValidateStarSelector { get; set; }
        [Category("Performance Warnings")]
        [DisplayName("Disallow units for 0 values")]
        [Description("Warn when units are unnecessarily specified for the number 0 (which never needs a unit in CSS).")]
        [DefaultValue(true)]
        public bool ValidateZeroUnit { get; set; }
        [Category("Performance Warnings")]
        [DisplayName("Small images should be inlined")]
        [Description("Warn on URLs to small images that are not embedded using data URIs.")]
        [DefaultValue(true)]
        public bool ValidateEmbedImages { get; set; }

        [Category("Performance Warnings")]
        [DisplayName("Disallow unrecognized vendor-specifics")]
        [Description("Warn on unrecognized vendor specific properties, pseudos, and @-directives.")]
        [DefaultValue(true)]
        public bool ValidateVendorSpecifics { get; set; }
        #endregion

        [Category("IntelliSense")]
        [DisplayName("Sync vendor-specific values")]
        [Description("Synchronize vendor-specific property values when modifying the standard property.")]
        [DefaultValue(true)]
        public bool SyncVendorValues { get; set; }

        [Category("IntelliSense")]
        [DisplayName("Sync embedded image values")]
        [Description("Synchronize base64 embedded background image property values when modifying the fallback property.")]
        [DefaultValue(true)]
        public bool SyncBase64ImageValues { get; set; }

        [Category("IntelliSense")]
        [DisplayName("Show initial/inherit")]
        [Description("Show the global property values 'initial' and 'inherit' in IntelliSense. Disabling this will not warn if you use them.")]
        [DefaultValue(false)]
        public bool ShowInitialInherit { get; set; }

        [Category("IntelliSense")]
        [DisplayName("Show unsupported properties")]
        [Description("Show property names, values, and pseudos that aren't supported by any browser yet.")]
        [DefaultValue(true)]
        public bool ShowUnsupported { get; set; }

        [Category("IntelliSense")]
        [DisplayName("Show browser support tooltips")]
        [Description("Show which browser support CSS properties & values on mouse hover.")]
        [DefaultValue(true)]
        public bool ShowBrowserTooltip { get; set; }

        #region Bundle Settings.

        [Category("Bundle")]
        [DisplayName("Minify files on making bundle")]
        [Description("Make minified version of the file when bundle is generated. The minified file would be updated if the auto-minfied option is enabled.")]
        [DefaultValue(false)]
        public bool MakeMinified { get; set; }

        [Category("Bundle")]
        [DisplayName("Adjust relative paths")]
        [Description("Adjust relative URLs when bundling CSS files to a different folder. Consider disabling this if image files do not share the same directory structure as CSS files.")]
        [DefaultValue(true)]
        public bool AdjustRelativePaths { get; set; }

        [Category("Bundle")]
        [DisplayName("Run on build")]
        [Description("Regenerate the sprite when building the solution.")]
        [DefaultValue(false)]
        public bool RunOnBuild { get; set; }

        [Category("Bundle")]
        [DisplayName("Custom CSS output directory")]
        [Description("Specifies a custom subfolder to save CSS files to. By default, compiled output will be placed in the same folder and nested under the original file.")]
        [DefaultValue(null)]
        public string OutputDirectory { get; set; }
        #endregion
    }

    public abstract class CompilationSettings<T> : SettingsBase<T>, ICompilerInvocationSettings, IMarginSettings, ICssSourceMapSettings where T : CompilationSettings<T>
    {
        [Category("Editor")]
        [DisplayName("Show preview pane")]
        [Description("Show a preview pane containing the compiled source in the editor.")]
        [DefaultValue(true)]
        public bool ShowPreviewPane { get; set; }

        [Category("Compilation")]
        [DisplayName("Compile files on save")]
        [Description("Compile files when saving them, if a compiled file already exists.")]
        [DefaultValue(true)]
        public bool CompileOnSave { get; set; }

        [Category("Compilation")]
        [DisplayName("Compile files on build")]
        [Description("Compile all files that have matching compiled files when building the project.")]
        [DefaultValue(false)]
        public bool CompileOnBuild { get; set; }

        [Category("Compilation")]
        [DisplayName("Custom output directory")]
        [Description("Specifies a custom subfolder to save compiled files to. By default, compiled output will be placed in the same folder and nested under the original file.")]
        [DefaultValue(null)]
        public string OutputDirectory { get; set; }

        [Category("Compilation")]
        [DisplayName("Create source map files")]
        [Description("Generate source map files when minifying. This option has no effect when Minify is disabled.")]
        [DefaultValue(true)]
        public bool GenerateSourceMaps { get; set; }

        [Category("Compilation")]
        [DisplayName("Process source maps")]
        [Description("Regardless of Create source map option, this will generate source map (or use the generated one) to bring about editor enhancement features; such as specificity, selector path and go-to-definition.")]
        [DefaultValue(true)]
        public bool ProcessSourceMapsForEditorEnhancements { get; set; }

        [Category("Compilation")]
        [DisplayName("Don't save raw compilation output")]
        [Description("Don't save separate unminified compiler output. This option has no effect when Minify On Save is disabled for the output format.")]
        [DefaultValue(false)]
        public bool MinifyInPlace { get; set; }

        [Category("Compilation")]
        [DisplayName("Strict Math")]
        [Description("With this option turned off, LESS will try and process all maths in your CSS.")]
        [DefaultValue(false)]
        public bool StrictMath { get; set; }
    }

    public abstract class ChainableCompilationSettings<T> : CompilationSettings<T>, IChainableCompilerSettings where T : ChainableCompilationSettings<T>
    {
        private bool enableChainCompilation;

        [Category("Compilation")]
        [DisplayName("Auto-compile dependent files on save")]
        [Description("Compile all files that depend @import the current file on save. This feature will only compile files that already have compiled output. This option has no effect when Compile on Save is disabled.")]
        [DefaultValue(true)]
        public bool EnableChainCompilation
        {
            get { return enableChainCompilation; }
            set { enableChainCompilation = value; OnEnableChainCompilationChanged(); }
        }
        public event EventHandler EnableChainCompilationChanged;
        void OnEnableChainCompilationChanged() { OnEnableChainCompilationChanged(EventArgs.Empty); }
        void OnEnableChainCompilationChanged(EventArgs e)
        {
            if (EnableChainCompilationChanged != null)
                EnableChainCompilationChanged(this, e);
        }
    }

    public sealed class LessSettings : ChainableCompilationSettings<LessSettings> { }

    public sealed class ScssSettings : ChainableCompilationSettings<ScssSettings> { }

    public sealed class CoffeeScriptSettings : CompilationSettings<CoffeeScriptSettings>, ILinterSettings
    {
        [DisplayName("Wrap generated JavaScript files")]
        [Description("Wrap the generated JavaScript source in an anonymous function. This prevents variables from leaking into the global scope.")]
        [Category("CoffeeScript")]
        [DefaultValue(true)]
        public bool WrapClosure { get; set; }

        [Category("Linter")]
        [DisplayName("Run on save")]
        [Description("Run linter when saving each source file.")]
        [DefaultValue(true)]
        public bool LintOnSave { get; set; }

        [Category("Linter")]
        [DisplayName("Run on build")]
        [Description("Lint all files when building the solution.")]
        [DefaultValue(false)]
        public bool LintOnBuild { get; set; }

        [Category("Linter")]
        [DisplayName("Results location")]
        [Description("Where to show messages from the linter.")]
        [DefaultValue(TaskErrorCategory.Message)]
        public TaskErrorCategory LintResultLocation { get; set; }
    }

    public sealed class LiveScriptSettings : CompilationSettings<LiveScriptSettings>
    {
        [DisplayName("Wrap generated JavaScript files")]
        [Description("Wrap the generated JavaScript source in an anonymous function. This prevents variables from leaking into the global scope.")]
        [Category("LiveScript")]
        [DefaultValue(true)]
        public bool WrapClosure { get; set; }
    }
    public sealed class SweetJsSettings : CompilationSettings<SweetJsSettings> { }

    public sealed class MarkdownSettings : SettingsBase<MarkdownSettings>, ICompilerInvocationSettings, IMarginSettings, IMarkdownOptions
    {
        #region Compilation
        [Category("Editor")]
        [DisplayName("Show preview pane")]
        [Description("Show a preview pane containing the rendered output in the editor.")]
        [DefaultValue(true)]
        public bool ShowPreviewPane { get; set; }

        [Category("Compilation")]
        [DisplayName("Compile files on save")]
        [Description("Compile files when saving them, if a compiled file already exists.")]
        [DefaultValue(true)]
        public bool CompileOnSave { get; set; }
        [Category("Compilation")]
        [DisplayName("Compile files on build")]
        [Description("Compile all files that have matching compiled files when building the project.")]
        [DefaultValue(false)]
        public bool CompileOnBuild { get; set; }

        [Category("Compilation")]
        [DisplayName("Custom output directory")]
        [Description("Specifies a custom subfolder to save compiled files to. By default, compiled output will be placed in the same folder and nested under the original file.")]
        [DefaultValue(null)]
        public string OutputDirectory { get; set; }
        #endregion

        [Category("Compile Options")]
        [DisplayName("Make bare URLs into hyperlinks")]
        [Description("When true, (most) bare plain Urls are auto-hyperlinked. WARNING: this is a significant deviation from the Markdown spec.")]
        [DefaultValue(false)]
        public bool AutoHyperlink { get; set; }

        [Category("Compile Options")]
        [DisplayName("Make bare emails into links")]
        [Description("When false, email addresses will never be auto-linked. WARNING: this is a significant deviation from the Markdown spec.")]
        [DefaultValue(false)]
        public bool LinkEmails { get; set; }

        [Category("Compile Options")]
        [DisplayName("Make return into a newline")]
        [Description("When true, RETURN becomes a literal newline. WARNING: this is a significant deviation from the Markdown spec.")]
        [DefaultValue(false)]
        public bool AutoNewLines { get; set; }

        [Category("Compile Options")]
        [DisplayName("Generate XHTML output")]
        [Description("When true, the output is valid XHTML. Otherwise regular HTML it output. In this case, when true (mostly) means that single tags are closed with `/>` instead of `>`.")]
        [DefaultValue(true)]
        public bool GenerateXHTML { get; set; }

        [Category("Compile Options")]
        [DisplayName("Encode problem Url characters")]
        [Description("When true, problematic Url characters like [, ], (, and so forth will be encoded. WARNING: this is a significant deviation from the Markdown spec.")]
        [DefaultValue(false)]
        public bool EncodeProblemUrlCharacters { get; set; }

        [Category("Compile Options")]
        [DisplayName("Require non-word characters for bold/italic")]
        [Description("When true, bold and italic require non-word characters on both sides. WARNING: this is a significant deviation from the Markdown spec.")]
        [DefaultValue(false)]
        public bool StrictBoldItalic { get; set; }
        string IMarkdownOptions.EmptyElementSuffix { get { return GenerateXHTML ? " />" : ">"; } }

        [Category("Compilation")]
        [DisplayName("Don't save raw compilation output")]
        [Description("Don't save separate unminified compiler output. This option has no effect when Minify On Save is disabled for HTML.")]
        [DefaultValue(false)]
        public bool MinifyInPlace { get; set; }
    }

    public interface IBundleSettings
    {
        bool MakeMinified { get; }
        bool RunOnBuild { get; }
        string OutputDirectory { get; }
    }

    public interface ISourceMapSettings
    {
        bool GenerateSourceMaps { get; }
    }

    public interface ICssSourceMapSettings : ISourceMapSettings
    {
        bool ProcessSourceMapsForEditorEnhancements { get; }
    }

    public interface IMarginSettings
    {
        bool ShowPreviewPane { get; }
    }

    public interface ICompilerInvocationSettings
    {
        bool CompileOnSave { get; }
        bool CompileOnBuild { get; }
        string OutputDirectory { get; }
        bool MinifyInPlace { get; }
    }

    public interface IChainableCompilerSettings : ICompilerInvocationSettings
    {
        bool EnableChainCompilation { get; }
        event EventHandler EnableChainCompilationChanged;
    }

    public interface IMinifierSettings
    {
        bool AutoMinify { get; set; }
        bool GzipMinifiedFiles { get; }
    }

    public enum WarningLocation
    {
        Warnings = 0,
        Messages = 1,
    }
}
