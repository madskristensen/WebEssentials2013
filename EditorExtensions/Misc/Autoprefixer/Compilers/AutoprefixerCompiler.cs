using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Settings;

namespace MadsKristensen.EditorExtensions.Autoprefixer
{
    [Export(typeof(NodeExecutorBase))]
    public class AutoprefixerCompiler : NodeExecutorBase
    {
        public override string ServiceName { get { return "Autoprefixer"; } }
        public override string TargetExtension { get { return null; } }
        public override bool MinifyInPlace { get { return false; } }
        public override bool GenerateSourceMap { get { return false; } }

        protected override string GetPath(string sourceFileName, string targetFileName)
        {
            var parameters = new NodeServerUtilities.Parameters();

            parameters.UriComponentsDictionary.Add("service", ServiceName);
            parameters.UriComponentsDictionary.Add("sourceFileName", sourceFileName);

            if (!string.IsNullOrWhiteSpace(WESettings.Instance.Css.AutoprefixerBrowsers))
                parameters.UriComponentsDictionary.Add("autoprefixerBrowsers", WESettings.Instance.Css.AutoprefixerBrowsers);

            return parameters.FlattenParameters();
        }
    }
}
