using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.LiveScript
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(LiveScriptContentTypeDefinition.LiveScriptContentType)]
    public class LiveScriptCompiler : JsCompilerBase
    {
        protected override bool Previewing { get { return WESettings.Instance.LiveScript.ShowPreviewPane; } }
        public override string ServiceName { get { return "LiveScript"; } }
        public override bool MinifyInPlace { get { return WESettings.Instance.SweetJs.MinifyInPlace; } }
        public override bool GenerateSourceMap { get { return false; /*WESettings.Instance.LiveScript.GenerateSourceMaps && !WESettings.Instance.LiveScript.MinifyInPlace;*/ } }
        // ^ Since maps aren't yet supported by LiveScript
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

            if (!WESettings.Instance.LiveScript.WrapClosure)
                parameters.Add("bare");

            return parameters.FlattenParameters();
        }
    }
}
