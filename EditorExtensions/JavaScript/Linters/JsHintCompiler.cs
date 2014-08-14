using System.Collections.Generic;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    public interface ILintCompiler
    {
        string ServiceName { get; }
        IEnumerable<string> SourceExtensions { get; }
        Task<CompilerResult> CheckAsync(string sourcePath);
    }

    public class JsHintCompiler : NodeExecutorBase, ILintCompiler
    {
        public static readonly string ConfigFileName = ".jshintrc";

        protected virtual string ConfigFile { get { return ConfigFileName; } }
        public virtual IEnumerable<string> SourceExtensions { get { return new[] { ".js" }; } }

        public override string ServiceName { get { return "JsHint"; } }
        public override string TargetExtension { get { return null; } }
        public override bool MinifyInPlace { get { return false; } }
        public override bool GenerateSourceMap { get { return false; } }

        public Task<CompilerResult> CheckAsync(string sourcePath)
        {
            return CompileAsync(sourcePath, null);
        }

        protected override string GetPath(string sourceFileName, string targetFileName)
        {
            GetOrCreateGlobalSettings(ConfigFile); // Ensure that default settings exist

            var parameters = new NodeServerUtilities.Parameters();

            parameters.Add("service", ServiceName);
            parameters.Add("sourceFileName", sourceFileName);

            return parameters.FlattenParameters();
        }

        protected override string PostProcessResult(string result, string targetFileName, string sourceFileName)
        {
            return result;
        }
    }
}
