using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType("IcedCoffeeScript")]
    public class IcedCoffeeScriptCompiler : CoffeeScriptCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\iced-coffee-script\bin\coffee");

        public override string ServiceName { get { return "IcedCoffeeScript"; } }
        protected override string CompilerPath { get { return _compilerPath; } }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            return "--runtime inline " + base.GetArguments(sourceFileName, targetFileName);
        }
    }
}