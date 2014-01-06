using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class IcedCoffeeScriptMargin : CoffeeScriptMargin
    {
        private static NodeExecutorBase _compiler = new IcedCoffeeScriptCompiler();

        protected override string ServiceName { get { return "IcedCoffeeScript"; } }
        protected override NodeExecutorBase Compiler { get { return _compiler; } }

        public IcedCoffeeScriptMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(contentType, source, showMargin, document)
        { }
    }
}