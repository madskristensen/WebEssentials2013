using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public class CssCompilerResult : CompilerResult
    {
        public Task<CssSourceMap> SourceMap { get; set; }

        private CssCompilerResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
            : base(sourceFileName, targetFileName, isSuccess, result, errors)
        { }

        public static CssCompilerResult GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
        {
            CssCompilerResult compilerResult = new CssCompilerResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, errors);

            if (mapFileName == null)
                return null;

            var extension = Path.GetExtension(sourceFileName).TrimStart('.');
            compilerResult.SourceMap = CssSourceMap.Create(targetFileName, mapFileName, Mef.GetContentType(extension));

            return compilerResult;
        }

        public static CssCompilerResult GenerateResultFromParent(CompilerResult compilerResult)
        {
            return new CssCompilerResult(compilerResult.SourceFileName, compilerResult.TargetFileName, null, compilerResult.IsSuccess, compilerResult.Result, compilerResult.Errors);
        }
    }
}