using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.CoffeeScript
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType("CoffeeScript")]
    public class CoffeeScriptCompiler : JsCompilerBase
    {
        protected override bool Previewing { get { return WESettings.Instance.CoffeeScript.ShowPreviewPane; } }
        public override string ServiceName { get { return "CoffeeScript"; } }
        public override bool MinifyInPlace { get { return WESettings.Instance.CoffeeScript.MinifyInPlace; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.CoffeeScript.GenerateSourceMaps && !MinifyInPlace; } }

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

            if (!WESettings.Instance.CoffeeScript.WrapClosure)
                parameters.Add("bare");

            return parameters.FlattenParameters();
        }
    }
}
