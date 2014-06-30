using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public class CssCompilerResult : CompilerResult
    {
        public string SourceMapData { get; set; }
        public Task<CssSourceMap> SourceMap { get; set; }

        private CssCompilerResult(string sourceFileName, string targetFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors, bool hasSkipped)
            : base(sourceFileName, targetFileName, isSuccess, result, errors, hasSkipped)
        { }

        public async static Task<CssCompilerResult> GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors, bool hasSkipped = false)
        {
            CssCompilerResult compilerResult = new CssCompilerResult(sourceFileName, targetFileName, isSuccess, result, errors, hasSkipped);

            if (mapFileName == null)
                return null;

            var extension = Path.GetExtension(sourceFileName).TrimStart('.');

            compilerResult.SourceMap = CssSourceMap.Create(await FileHelpers.ReadAllTextRetry(targetFileName),
                                                           await FileHelpers.ReadAllTextRetry(mapFileName),
                                                           Path.GetDirectoryName(targetFileName),
                                                           Mef.GetContentType(extension));

            return compilerResult;
        }
    }
}