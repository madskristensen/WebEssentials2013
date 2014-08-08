using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.SweetJs
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(SweetJsContentTypeDefinition.SweetJsContentType)]
    public class SweetJsCompiler : JsCompilerBase
    {
        protected override bool Previewing { get { return WESettings.Instance.SweetJs.ShowPreviewPane; } }
        public override string ServiceName { get { return SweetJsContentTypeDefinition.SweetJsContentType; } }
        public override bool MinifyInPlace { get { return WESettings.Instance.SweetJs.MinifyInPlace; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.SweetJs.GenerateSourceMaps && !MinifyInPlace; } }

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

            return parameters.FlattenParameters();
        }
    }
}
