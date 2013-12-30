using System.IO;

namespace MadsKristensen.EditorExtensions
{
    public class IcedCoffeeScriptCompiler : CoffeeScriptCompiler
    {
        protected override string ServiceName
        {
            get { return "IcedCoffeeScript"; }
        }
        protected override string CompilerPath
        {
            get { return @"node_modules\iced-coffee-script\bin\coffee"; }
        }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            return "--runtime inline " + base.GetArguments(sourceFileName, targetFileName);
        }

        protected override string PostProcessResult(string resultSource, string sourceFileName, string targetFileName)
        {
            Logger.Log("IcedCoffeeScript: " + Path.GetFileName(sourceFileName) + " compiled.");
            RenameMapFile(targetFileName);

            return resultSource;
        }
    }
}