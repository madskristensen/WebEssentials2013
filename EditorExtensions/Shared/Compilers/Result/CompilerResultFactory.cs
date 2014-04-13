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
            return await GenerateResult(sourceFileName, targetFileName, null, true, null, null);
        }

        public async static Task<CompilerResult> GenerateResult(string sourceFileName, string targetFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
        {
            return await GenerateResult(sourceFileName, targetFileName, null, isSuccess, result, errors);
        }

        public async static Task<CompilerResult> GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
        {
            CompilerResult instance;

            mapFileName = mapFileName ?? targetFileName + ".map";

            if (result == null && File.Exists(targetFileName))
                result = await FileHelpers.ReadAllTextRetry(targetFileName);

            if (targetFileName != null && Path.GetExtension(targetFileName).Equals(".css", StringComparison.OrdinalIgnoreCase) && File.Exists(mapFileName))
                instance = await CssCompilerResult.GenerateResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, errors);
            else
                instance = CompilerResult.GenerateResult(sourceFileName, targetFileName, isSuccess, result, errors);

            return instance;
        }
    }
}