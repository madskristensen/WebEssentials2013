using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public Task<CompilerResult> Check(string sourcePath)
        {
            return Compile(sourcePath, null);
        }

        private static string FindLocalSettings(string sourcePath)
        {
            string dir = Path.GetDirectoryName(sourcePath);

            while (!File.Exists(Path.Combine(dir, ".jshintrc")))
            {
                dir = Path.GetDirectoryName(dir);
                if (String.IsNullOrEmpty(dir))
                    return null;
            }
            return Path.Combine(dir, ".jshintrc");
        }

        public static string GetOrCreateGlobalSettings()
        {
            string userPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".jshintrc");

            if (!File.Exists(userPath))
            {
                string extensionDir = Path.GetDirectoryName(typeof(JsHintCompiler).Assembly.Location);
                string fullPath = Path.Combine(extensionDir, @"Resources\settings-defaults\.jshintrc");
                File.Copy(fullPath, userPath);
            }

            return userPath;
        }

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

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            return String.Format(CultureInfo.CurrentCulture, "--config \"{0}\" --reporter \"{1}\" \"{2}\""
                               , FindLocalSettings(sourceFileName) ?? GetOrCreateGlobalSettings()
                               , _jsHintReporter
                               , sourceFileName);
        }

        protected override string PostProcessResult(string resultSource, string sourceFileName, string targetFileName)
        {
            Logger.Log("JSHint: " + Path.GetFileName(sourceFileName) + " checked.");

            return resultSource;
        }
    }
}