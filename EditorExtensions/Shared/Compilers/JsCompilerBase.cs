
namespace MadsKristensen.EditorExtensions
{
    public abstract class JsCompilerBase : NodeExecutorBase
    {
        public override string TargetExtension { get { return ".js"; } }
    }
}
