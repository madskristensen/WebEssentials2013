using System;
using System.Globalization;
using System.IO;

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
    }
}