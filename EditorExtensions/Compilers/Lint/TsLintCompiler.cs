using System;
using System.Globalization;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    public class TsLintCompiler : JsHintCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\tslint\bin\tslint");
        private static readonly string _tsLintFormatterDirectory = Path.Combine(WebEssentialsResourceDirectory, @"Scripts");
        private const string _tsLintFormatter = "tslint";
        private const string _settingsName = "tslint.json";

        public override string SourceExtension { get { return ".ts"; } }
        public override string ServiceName { get { return "TsLint"; } }
        protected override string CompilerPath { get { return _compilerPath; } }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            GetOrCreateGlobalSettings(_settingsName); // Ensure that default settings exist

            return String.Format(CultureInfo.CurrentCulture, "--formatters-dir \"{0}\" --format \"{1}\" --config \"{2}\" --file \"{3}\""
                               , _tsLintFormatterDirectory
                               , _tsLintFormatter
                               , FindLocalSettings(sourceFileName, _settingsName) ?? GetOrCreateGlobalSettings("tslint.json")
                               , sourceFileName);
        }

        protected static string FindLocalSettings(string sourcePath, string settingsName)
        {
            string dir = Path.GetDirectoryName(sourcePath);

            while (!File.Exists(Path.Combine(dir, settingsName)))
            {
                dir = Path.GetDirectoryName(dir);
                if (String.IsNullOrEmpty(dir))
                    return null;
            }

            return Path.Combine(dir, settingsName);
        }
    }
}