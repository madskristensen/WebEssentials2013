using System.ComponentModel.Composition;
using System.Globalization;
using MadsKristensen.EditorExtensions.RtlCss;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

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

        protected override string GetPath(string sourceFileName, string targetFileName)
        {
            GetOrCreateGlobalSettings(RtlCssCompiler.ConfigFileName);

            string outputStyle = WESettings.Instance.Scss.OutputStyle.ToString().ToLowerInvariant();
            string numberPrecision = WESettings.Instance.Scss.NumberPrecision.ToString(CultureInfo.InvariantCulture);

            var parameters = new NodeServerUtilities.Parameters();

            parameters.Add("service", ServiceName);
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
