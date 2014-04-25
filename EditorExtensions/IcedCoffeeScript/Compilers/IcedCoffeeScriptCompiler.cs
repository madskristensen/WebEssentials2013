using System.ComponentModel.Composition;
using System.IO;
using MadsKristensen.EditorExtensions.CoffeeScript;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.IcedCoffeeScript
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType(IcedCoffeeScriptContentTypeDefinition.IcedCoffeeScriptContentType)]
    public class IcedCoffeeScriptCompiler : CoffeeScriptCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\iced-coffee-script\bin\coffee");

        public override string ServiceName { get { return "IcedCoffeeScript"; } }
        protected override string CompilerPath { get { return _compilerPath; } }

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            return "--runtime inline " + base.GetArguments(sourceFileName, targetFileName, mapFileName);
        }
    }
}