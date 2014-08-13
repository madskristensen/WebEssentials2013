using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public class CssCompilerResult : CompilerResult
    {
        public Task<CssSourceMap> SourceMap { get; set; }

        private CssCompilerResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasSkipped, string rtlSourceFileName, string rtlTargetFileName, string rtlMapFileName, string rtlResult, string rtlResultMap)
            : base(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, hasSkipped, rtlSourceFileName, rtlTargetFileName, rtlMapFileName, rtlResult, rtlResultMap)
        { }

        public static CssCompilerResult GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, string rtlSourceFileName, string rtlTargetFileName, string rtlMapFileName, string rtlResult, string rtlResultMap, bool hasSkipped = false)
        {
            CssCompilerResult compilerResult = new CssCompilerResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, hasSkipped, rtlSourceFileName, rtlTargetFileName, rtlMapFileName, rtlResult, rtlResultMap);

            if (mapFileName == null)
                return compilerResult;

            string extension = Path.GetExtension(sourceFileName).TrimStart('.');

            compilerResult.SourceMap = CssSourceMap.Create(result, resultMap, Path.GetDirectoryName(targetFileName), Mef.GetContentType(extension));

            return compilerResult;
        }
    }
}
