using System.ComponentModel.Composition;
using System.Globalization;
using MadsKristensen.EditorExtensions.RtlCss;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using System.Threading.Tasks;
using System.Web;

namespace MadsKristensen.EditorExtensions.Scss
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(ScssContentTypeDefinition.ScssContentType)]
    public class ScssCompiler : CssCompilerBase
    {
        protected override bool Previewing { get { return WESettings.Instance.Scss.ShowPreviewPane; } }
        public override string ServiceName { get { return "SCSS"; } }
        public override string TargetExtension { get { return ".css"; } }
        public override bool MinifyInPlace { get { return WESettings.Instance.Scss.MinifyInPlace; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.Scss.GenerateSourceMaps && !MinifyInPlace; } }

        public override async Task<CompilerResult> CompileAsync(string sourceFileName, string targetFileName)
        {
            if (WESettings.Instance.Scss.UseRubyRuntime)
            {
                await RubyScssServer.Up();
            }

            return await base.CompileAsync(sourceFileName, targetFileName);
        }

        protected override string GetPath(string sourceFileName, string targetFileName)
        {
            GetOrCreateGlobalSettings(RtlCssCompiler.ConfigFileName);

            string outputStyle = WESettings.Instance.Scss.OutputStyle.ToString().ToLowerInvariant();
            string numberPrecision = WESettings.Instance.Scss.NumberPrecision.ToString(CultureInfo.InvariantCulture);

            var parameters = new NodeServerUtilities.Parameters();

            if (!WESettings.Instance.Scss.UseRubyRuntime)
            {
                parameters.Add("service", ServiceName);
            }
            else
            {
                parameters.Add("service", "RubySCSS");
                parameters.Add("rubyAuth",  HttpUtility.UrlEncode(RubyScssServer.AuthenticationToken));
                parameters.Add("rubyPort", RubyScssServer.Port.ToString());
            }

            parameters.Add("sourceFileName", sourceFileName);
            parameters.Add("targetFileName", targetFileName);
            parameters.Add("mapFileName", targetFileName + ".map");
            parameters.Add("precision", numberPrecision);
            parameters.Add("outputStyle", outputStyle);

            if (GenerateSourceMap)
                parameters.Add("sourceMapURL");

            if (WESettings.Instance.Css.Autoprefix)
            {
                parameters.Add("autoprefixer");

                if (!string.IsNullOrWhiteSpace(WESettings.Instance.Css.AutoprefixerBrowsers))
                    parameters.UriComponentsDictionary.Add("autoprefixerBrowsers", WESettings.Instance.Css.AutoprefixerBrowsers);
            }

            if (WESettings.Instance.Css.RtlCss)
                parameters.Add("rtlcss");

            return parameters.FlattenParameters();
        }
    }
}
