using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    internal class CoffeeScriptProjectCompiler : ProjectCompilerBase
    {
        protected override string ServiceName
        {
            get { return "CoffeeScript"; }
        }

        protected override string CompileToExtension
        {
            get { return ".js"; }
        }

        protected override string CompileToLocation
        {
            get { return WESettings.GetString(WESettings.Keys.CoffeeScriptCompileToLocation); }
        }

        protected override NodeExecutorBase Compiler
        {
            get { return new CoffeeScriptCompiler(); }
        }

        protected override IEnumerable<string> Extensions
        {
            get
            {
                return new string[] { ".coffee" };
            }
        }
    }
}
