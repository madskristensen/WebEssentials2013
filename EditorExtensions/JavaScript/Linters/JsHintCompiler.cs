using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    public interface ILintCompiler
    {
        Task<CompilerResult> CheckAsync(string sourcePath);
        string ServiceName { get; }
        IEnumerable<string> SourceExtensions { get; }
    }

    public class JsHintCompiler : NodeExecutorBase, ILintCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\jshint\bin\jshint");
        // JsHint Reported is located in Resources\Scripts\ directory. Read more at http://www.jshint.com/docs/reporters/
        private static readonly string _reporter = Path.Combine(WebEssentialsResourceDirectory, @"Scripts\jshintReporter.js");
        public static readonly string ConfigFileName = ".jshintrc";

        public override string TargetExtension { get { return null; } }
        public virtual IEnumerable<string> SourceExtensions { get { return new[] { ".js" }; } }
        public override string ServiceName { get { return "JsHint"; } }
        public override bool GenerateSourceMap { get { return false; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        protected override Func<string, IEnumerable<CompilerError>> ParseErrors
        {
            get { return ParseErrorsWithJson; }
        }

        public Task<CompilerResult> CheckAsync(string sourcePath)
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

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            GetOrCreateGlobalSettings(ConfigFileName); // Ensure that default settings exist

            return String.Format(CultureInfo.CurrentCulture, "--reporter \"{0}\" \"{1}\""
                               , _reporter
                               , sourceFileName);
        }

        protected async override Task<string> PostProcessResult(string resultSource, string sourceFileName, string targetFileName, string mapFileName)
        {
            return await Task.FromResult(resultSource);
        }
    }
}
