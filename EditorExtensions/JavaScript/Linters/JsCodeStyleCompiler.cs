using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    public class JsCodeStyleCompiler : JsHintCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\jscs\bin\jscs");
        private static readonly string _reporter = Path.Combine(WebEssentialsResourceDirectory, @"Scripts\jscsReporter.js");
        public new static readonly string ConfigFileName = ".jscsrc";

        public override IEnumerable<string> SourceExtensions { get { return new[] { ".js" }; } }
        public override string ServiceName { get { return "JSCS"; } }
        protected override string CompilerPath { get { return _compilerPath; } }

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            GetOrCreateGlobalSettings(ConfigFileName); // Ensure that default settings exist

            return String.Format(CultureInfo.CurrentCulture, "--reporter \"{0}\" --config \"{1}\" \"{2}\""
                               , _reporter
                               , FindLocalSettings(sourceFileName, ConfigFileName) ?? GetOrCreateGlobalSettings(ConfigFileName)
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