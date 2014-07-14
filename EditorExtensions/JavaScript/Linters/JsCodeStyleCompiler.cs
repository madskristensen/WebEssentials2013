
namespace MadsKristensen.EditorExtensions.JavaScript
{
    public class JsCodeStyleCompiler : JsHintCompiler
    {
        public new static readonly string ConfigFileName = ".jscsrc";

        public override string ServiceName { get { return "JSCS"; } }
    }
}
