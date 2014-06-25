using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.JavaScript;

namespace MadsKristensen.EditorExtensions.TypeScript
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

        protected override Task<string> GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            GetOrCreateGlobalSettings(ConfigFileName); // Ensure that default settings exist

            return Task.FromResult(string.Format(CultureInfo.CurrentCulture, "--formatters-dir \"{0}\" --format \"{1}\" --file \"{2}\"",
                                   _tsLintFormatterDirectory,
                                   _tsLintFormatter,
                                   sourceFileName));
        }
    }
}