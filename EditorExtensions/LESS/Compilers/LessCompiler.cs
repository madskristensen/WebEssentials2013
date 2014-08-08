using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Less
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType("LESS")]
    public class LessCompiler : CssCompilerBase
    {
        protected override bool Previewing { get { return WESettings.Instance.Less.ShowPreviewPane; } }
        public override string TargetExtension { get { return ".css"; } }
        public override string ServiceName { get { return "LESS"; } }
        public override bool MinifyInPlace { get { return WESettings.Instance.Less.MinifyInPlace; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.Less.GenerateSourceMaps && !MinifyInPlace; } }

        protected override string GetPath(string sourceFileName, string targetFileName)
        {
            string mapFileName = targetFileName + ".map";
            var parameters = new NodeServerUtilities.Parameters();

            parameters.Add("service", ServiceName);
            parameters.Add("sourceFileName", sourceFileName);
            parameters.Add("targetFileName", targetFileName);
            parameters.Add("mapFileName", mapFileName);

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

            return parameters.FlattenParameters();
        }
    }
}
