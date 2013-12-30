using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    internal class IcedCoffeeScriptProjectCompiler : CoffeeScriptProjectCompiler
    {
        protected override string ServiceName
        {
            get { return "IcedCoffeeScript"; }
        }

        protected override NodeExecutorBase Compiler
        {
            get { return new IcedCoffeeScriptCompiler(); }
        }

        protected override IEnumerable<string> Extensions
        {
            get
            {
                return new string[] { ".iced" };
            }
        }
    }
}
