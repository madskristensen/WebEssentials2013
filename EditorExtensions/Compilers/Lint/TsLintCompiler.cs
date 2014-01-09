using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Helpers;

namespace MadsKristensen.EditorExtensions
{
    public class TsLintCompiler : JsHintCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\tslint\bin\tslint");

        protected override string ServiceName
        {
            get { return "TsLint"; }
        }
        protected override string CompilerPath
        {
            get { return _compilerPath; }
        }
        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            return String.Format(CultureInfo.CurrentCulture, "--format \"json\" --config \"{0}\" --file \"{1}\""
                               , FindLocalSettings(sourceFileName) ?? GlobalSettings(ServiceName)
                               , sourceFileName);
        }

        protected override IEnumerable<CompilerError> ParseErrorsWithJson(string error)
        {
            if (string.IsNullOrEmpty(error))
                return null;

            try
            {
                TsLintCompilerError[] results = Json.Decode<TsLintCompilerError[]>(error);

                if (results.Length == 0)
                    Logger.Log(ServiceName + " parse error: " + error);

                return TsLintCompilerError.ToCompilerError(results);
            }
            catch (ArgumentException)
            {
                Logger.Log(ServiceName + " parse error: " + error);
                return new[] { new CompilerError() { Message = error } };
            }
        }
    }
}