using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    public class TsLintCompiler : JsHintCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\tslint\bin\tslint");
        private static readonly string _tsLintFormatterDirectory = Path.Combine(WebEssentialsResourceDirectory, @"Scripts");
        private const string _tsLintFormatter = "tslint";
        public new readonly static string ConfigFileName = "tslint.json";

        public override IEnumerable<string> SourceExtensions { get { return new[] { ".ts" }; } }
        public override string ServiceName { get { return "TsLint"; } }
        protected override string CompilerPath { get { return _compilerPath; } }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            GetOrCreateGlobalSettings(ConfigFileName); // Ensure that default settings exist

            return String.Format(CultureInfo.CurrentCulture, "--formatters-dir \"{0}\" --format \"{1}\" --file \"{2}\""
                               , _tsLintFormatterDirectory
                               , _tsLintFormatter
                               , sourceFileName);
        }
    }
}