
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
    }
}