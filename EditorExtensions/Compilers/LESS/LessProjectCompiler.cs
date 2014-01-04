using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    internal class LessProjectCompiler : ProjectCompilerBase
    {
        protected override string ServiceName
        {
            get { return "LESS"; }
        }

        protected override string CompileToExtension
        {
            get { return ".css"; }
        }

        protected override string CompileToLocation
        {
            get { return WESettings.GetString(WESettings.Keys.LessCompileToLocation); }
        }

        protected override NodeExecutorBase Compiler
        {
            get { return new LessCompiler(); }
        }

        protected override IEnumerable<string> Extensions
        {
            get
            {
                return new string[] { ".less" };
            }
        }
    }
}