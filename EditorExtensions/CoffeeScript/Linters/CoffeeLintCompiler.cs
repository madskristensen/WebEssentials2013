using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MadsKristensen.EditorExtensions.JavaScript;

namespace MadsKristensen.EditorExtensions.CoffeeScript
{
    public class CoffeeLintCompiler : JsHintCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\coffeelint\bin\coffeelint");
        private static readonly string _reporter = Path.Combine(WebEssentialsResourceDirectory, @"Scripts\coffeeReporter.js");
        public new readonly static string ConfigFileName = "coffeelint.json";

        public override IEnumerable<string> SourceExtensions { get { return new[] { ".coffee", ".iced" }; } }
        public override string ServiceName { get { return "CoffeeLint"; } }
        protected override string CompilerPath { get { return _compilerPath; } }

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            GetOrCreateGlobalSettings(ConfigFileName); // Ensure that default settings exist

            return String.Format(CultureInfo.CurrentCulture, "--reporter \"{0}\" \"{1}\""
                               , _reporter
                               , sourceFileName);
        }
    }
}