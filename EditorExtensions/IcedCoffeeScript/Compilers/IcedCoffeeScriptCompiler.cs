using System.ComponentModel.Composition;
using MadsKristensen.EditorExtensions.CoffeeScript;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.IcedCoffeeScript
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(IcedCoffeeScriptContentTypeDefinition.IcedCoffeeScriptContentType)]
    public class IcedCoffeeScriptCompiler : CoffeeScriptCompiler
    {
        public override string ServiceName { get { return "IcedCoffeeScript"; } }

        protected override string GetPath(string sourceFileName, string targetFileName)
        {
            return base.GetPath(sourceFileName, targetFileName) + "runtime=inline";
        }
    }
}
