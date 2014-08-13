using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.RtlCss
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType("CSS")]
    public class RtlCssCompiler : NodeExecutorBase
    {
        public static readonly string ConfigFileName = ".rtlcssrc";

        public override string ServiceName { get { return "RTLCss"; } }
        public override string TargetExtension { get { return ".css"; } }
        public override bool MinifyInPlace { get { return false; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.Css.GenerateRtlSourceMaps; } }

        protected override string GetPath(string sourceFileName, string targetFileName)
        {
            GetOrCreateGlobalSettings(ConfigFileName); // Ensure that default settings exist

            var parameters = new NodeServerUtilities.Parameters();

            parameters.UriComponentsDictionary.Add("service", ServiceName);
            parameters.UriComponentsDictionary.Add("sourceFileName", sourceFileName);
            parameters.UriComponentsDictionary.Add("targetFileName", targetFileName);
            parameters.Add("mapFileName", targetFileName + ".map");

            if (WESettings.Instance.Css.RtlCss)
                parameters.Add("rtlcss");

            if (WESettings.Instance.Css.Autoprefix)
            {
                parameters.Add("autoprefixer");

                if (!string.IsNullOrWhiteSpace(WESettings.Instance.Css.AutoprefixerBrowsers))
                    parameters.Add("autoprefixerBrowsers", WESettings.Instance.Css.AutoprefixerBrowsers);
            }

            return parameters.FlattenParameters();
        }
    }
}
