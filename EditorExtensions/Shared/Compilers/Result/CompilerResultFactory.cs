using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions
{
    public static class CompilerResultFactory
    {
        public async static Task<CompilerResult> GenerateResult(string sourceFileName, string targetFileName)
        {
            return await GenerateResult(sourceFileName, targetFileName, null, true, null, null, null);
        }

        public async static Task<CompilerResult> GenerateResult(string sourceFileName, string targetFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
        {
            return await GenerateResult(sourceFileName, targetFileName, null, isSuccess, result, null, errors);
        }

        public async static Task<CompilerResult> GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasResult = false, string rtlSourceFileName = "", string rtlTargetFileName = "", string rtlMapFileName = "", string rtlResult = "", string rtlResultMap = "")
        {
            CompilerResult instance;

            mapFileName = mapFileName ?? targetFileName + ".map";

            if (result == null && File.Exists(targetFileName))
                result = await FileHelpers.ReadAllTextRetry(targetFileName);

            if (targetFileName != null && Path.GetExtension(targetFileName).Equals(".css", StringComparison.OrdinalIgnoreCase))
                instance = CssCompilerResult.GenerateResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, rtlSourceFileName, rtlTargetFileName, rtlMapFileName, rtlResult, rtlResultMap, hasResult);
            else
                instance = CompilerResult.GenerateResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, hasResult);

            return instance;
        }
    }
}
