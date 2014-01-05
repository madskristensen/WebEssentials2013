using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions
{
    internal class SassProjectCompiler : ProjectCompilerBase
    {
        protected override string ServiceName
        {
            get { return "SASS"; }
        }

        protected override string CompileToExtension
        {
            get { return ".css"; }
        }

        protected override string CompileToLocation
        {
            get { return WESettings.GetString(WESettings.Keys.SassCompileToLocation); }
        }

        protected override NodeExecutorBase Compiler
        {
            get { return new SassCompiler(); }
        }

        protected override IEnumerable<string> Extensions
        {
            get
            {
                return new string[] { ".scss" };
            }
        }
    }
}