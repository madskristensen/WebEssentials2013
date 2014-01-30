using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MadsKristensen.EditorExtensions
{
    public class JsCodeStyleCompiler : TsLintCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\jscs\bin\jscs");
        private static readonly string _jscsReporter = Path.Combine(WebEssentialsResourceDirectory, @"Scripts\jscs-node-reporter.js");
        private const string _settingsName = ".jscs.json";

        public override string SourceExtension { get { return ".js"; } }
        public override string ServiceName { get { return "JSCS"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        protected override Func<string, IEnumerable<CompilerError>> ParseErrors
        {
            get { return ParseErrorsWithXml; }
        }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            GetOrCreateGlobalSettings(_settingsName); // Ensure that default settings exist

            return String.Format(CultureInfo.CurrentCulture, "--reporter \"{0}\" --config \"{1}\" \"{2}\""
                               , "checkstyle"//_jscsReporter //github.com/mdevils/node-jscs/issues/211
                               , FindLocalSettings(sourceFileName, _settingsName) ?? GetOrCreateGlobalSettings(".jscs.json")
                               , sourceFileName);
        }

        private IEnumerable<CompilerError> ParseErrorsWithXml(string error)
        {
            try
            {
                return XDocument.Parse(error).Descendants("file").Select(file =>
                {
                    var fileName = file.Attribute("name").Value;
                    return file.Descendants("error").Select(e => new CompilerError
                               {
                                   FileName = fileName,
                                   Column = int.Parse(e.Attribute("column").Value, CultureInfo.InvariantCulture),
                                   Line = int.Parse(e.Attribute("line").Value, CultureInfo.InvariantCulture),
                                   Message = ServiceName + ": " + e.Attribute("message").Value
                               });
                }).First();
            }
            catch
            {
                Logger.Log(ServiceName + " parse error: " + error);
                return new[] { new CompilerError() { Message = error } };
            }
        }
    }
}