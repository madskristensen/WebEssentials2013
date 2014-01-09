using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public class JsHintCompiler : NodeExecutorBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\jshint\bin\jshint");
        // JsHint Reported is located in Resources\Scripts\ directory. Read more at http://www.jshint.com/docs/reporters/
        private static readonly string _jsHintReporter = Path.Combine(WebEssentialsResourceDirectory, @"Scripts\jshint-node-reporter.js");

        protected override string ServiceName
        {
            get { return "JsHint"; }
        }
        protected override string CompilerPath
        {
            get { return _compilerPath; }
        }
        protected override Func<string, IEnumerable<CompilerError>> ParseErrors
        {
            get { return ParseErrorsWithJson; }
        }

        public static string GlobalSettings(string serviceName)
        {
            var serviceNameLower = serviceName.ToLower(CultureInfo.CurrentCulture);

            string jsHintRc = Path.Combine(Settings.GetWebEssentialsSettingsFolder(), "." + serviceNameLower + "rc");

            if (!File.Exists(jsHintRc))
                File.Copy(Path.Combine(Path.GetDirectoryName(typeof(LessCompiler).Assembly.Location), @"Resources\settings-defaults\." + serviceNameLower + "rc")
                        , jsHintRc);

            return jsHintRc;
        }

        public Task<CompilerResult> Check(string sourcePath)
        {
            return Compile(sourcePath, null);
        }

        protected string FindLocalSettings(string sourcePath)
        {
            string dir = Path.GetDirectoryName(sourcePath);

            while (!File.Exists(Path.Combine(dir, "." + ServiceName.ToLower(CultureInfo.CurrentCulture) + "rc")))
            {
                dir = Path.GetDirectoryName(dir);
                if (String.IsNullOrEmpty(dir))
                    return null;
            }

            return Path.Combine(dir, "." + ServiceName.ToLower(CultureInfo.CurrentCulture) + "rc");
        }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            return String.Format(CultureInfo.CurrentCulture, "--config \"{0}\" --reporter \"{1}\" \"{2}\""
                               , FindLocalSettings(sourceFileName) ?? GlobalSettings(ServiceName)
                               , _jsHintReporter
                               , sourceFileName);
        }

        protected override string PostProcessResult(string resultSource, string sourceFileName, string targetFileName)
        {
            Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " checked.");

            return resultSource;
        }
    }
}
