using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public class CssCompilerResult : CompilerResult
    {
        public Task<CssSourceMap> SourceMap { get; set; }

        private CssCompilerResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasSkipped)
            : base(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, hasSkipped)
        { }

        public static CssCompilerResult GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasSkipped = false)
        {
            CssCompilerResult compilerResult = new CssCompilerResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, hasSkipped);

            if (mapFileName == null)
                return compilerResult;

            string extension = Path.GetExtension(sourceFileName).TrimStart('.');

            compilerResult.SourceMap = CssSourceMap.Create(result, resultMap, Path.GetDirectoryName(targetFileName), Mef.GetContentType(extension));

            return compilerResult;
        }
    }
}