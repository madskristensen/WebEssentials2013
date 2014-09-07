using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.RtlCss;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Less
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(LessContentTypeDefinition.LessContentType)]
    public class LessCompiler : CssCompilerBase
    {
        protected override bool Previewing { get { return WESettings.Instance.Less.ShowPreviewPane; } }
        public override string TargetExtension { get { return ".css"; } }
        public override string ServiceName { get { return "LESS"; } }
        public override bool MinifyInPlace { get { return WESettings.Instance.Less.MinifyInPlace; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.Less.GenerateSourceMaps && !MinifyInPlace; } }

        protected override string GetPath(string sourceFileName, string targetFileName)
        {
            GetOrCreateGlobalSettings(RtlCssCompiler.ConfigFileName);

            var parameters = new NodeServerUtilities.Parameters();

            parameters.Add("service", ServiceName);
            parameters.Add("sourceFileName", sourceFileName);
            parameters.Add("targetFileName", targetFileName);
            parameters.Add("mapFileName", targetFileName + ".map");

            if (GenerateSourceMap)
                parameters.Add("sourceMapURL");

            if (WESettings.Instance.Less.StrictMath)
                parameters.Add("strictMath");

            if (WESettings.Instance.Css.Autoprefix)
            {
                parameters.Add("autoprefixer");

                if (!string.IsNullOrWhiteSpace(WESettings.Instance.Css.AutoprefixerBrowsers))
                    parameters.Add("autoprefixerBrowsers", WESettings.Instance.Css.AutoprefixerBrowsers);
            }

            if (WESettings.Instance.Css.RtlCss)
                parameters.Add("rtlcss");

            return parameters.FlattenParameters();
        }
    }
}
