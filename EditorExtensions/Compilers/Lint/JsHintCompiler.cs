using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public interface ILintCompiler
    {
        Task<CompilerResult> Check(string sourcePath);
        string ServiceName { get; }
        string SourceExtension { get; }
    }

    public class JsHintCompiler : NodeExecutorBase, ILintCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\jshint\bin\jshint");
        // JsHint Reported is located in Resources\Scripts\ directory. Read more at http://www.jshint.com/docs/reporters/
        private static readonly string _jsHintReporter = Path.Combine(WebEssentialsResourceDirectory, @"Scripts\jshint-node-reporter.js");

        public override string TargetExtension { get { return null; } }
        public virtual string SourceExtension { get { return ".js"; } }
        public override string ServiceName { get { return "JsHint"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        protected override Func<string, IEnumerable<CompilerError>> ParseErrors
        {
            get { return ParseErrorsWithJson; }
        }

        public Task<CompilerResult> Check(string sourcePath)
        {
            return CompileAsync(sourcePath, null);
        }

        public static string GetOrCreateGlobalSettings(string fileName)
        {
            string globalFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fileName);

            if (!File.Exists(globalFile))
            {
                string extensionDir = Path.GetDirectoryName(typeof(JsHintCompiler).Assembly.Location);
                string settingsFile = Path.Combine(extensionDir, @"Resources\settings-defaults\", fileName);
                File.Copy(settingsFile, globalFile);
            }

            return globalFile;
        }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            GetOrCreateGlobalSettings(".jshintrc"); // Ensure that default settings exist

            return String.Format(CultureInfo.CurrentCulture, "--reporter \"{0}\" \"{1}\""
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
