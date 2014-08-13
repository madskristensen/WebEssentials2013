using System.Collections.Generic;
using MadsKristensen.EditorExtensions.JavaScript;

namespace MadsKristensen.EditorExtensions.CoffeeScript
{
    public class CoffeeLintCompiler : JsHintCompiler
    {
        public new readonly static string ConfigFileName = "coffeelint.json";

        protected override string ConfigFile { get { return ConfigFileName; } }
        public override IEnumerable<string> SourceExtensions { get { return new[] { ".coffee", ".iced" }; } }
        public override string ServiceName { get { return "CoffeeLint"; } }
    }
}
