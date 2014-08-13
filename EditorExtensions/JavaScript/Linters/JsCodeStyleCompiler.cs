
namespace MadsKristensen.EditorExtensions.JavaScript
{
    public class JsCodeStyleCompiler : JsHintCompiler
    {
        public new static readonly string ConfigFileName = ".jscsrc";

        protected override string ConfigFile { get { return ConfigFileName; } }
        public override string ServiceName { get { return "JSCS"; } }
    }
}
