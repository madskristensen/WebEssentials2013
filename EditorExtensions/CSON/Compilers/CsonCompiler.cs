using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.CoffeeScript
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(CsonContentTypeDefinition.CsonContentType)]
    public class CsonCompiler : NodeExecutorBase
    {
        public override string TargetExtension { get { return ".json"; } }
        public override string ServiceName { get { return "CSON"; } }
        public override bool MinifyInPlace { get { return false; } }
        public override bool GenerateSourceMap { get { return false; } }

        protected override string GetPath(string sourceFileName, string targetFileName)
        {
            var parameters = new NodeServerUtilities.Parameters();

            parameters.Add("service", ServiceName);
            parameters.Add("sourceFileName", sourceFileName);
            parameters.Add("targetFileName", targetFileName);

            return parameters.FlattenParameters();
        }
    }
}
