using System;
using System.Collections.Generic;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    public static class CompilerResultFactory
    {
        public static CompilerResult GenerateResult(string sourceFileName, string targetFileName)
        {
            return GenerateResult(sourceFileName, targetFileName, true, null, null);
        }

        public static CompilerResult GenerateResult(string sourceFileName, string targetFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
        {
            CompilerResult instance;

            var mapFileName = targetFileName + ".map";

            if (targetFileName != null && Path.GetExtension(targetFileName).Equals(".css", StringComparison.OrdinalIgnoreCase) && File.Exists(mapFileName))
                instance = CssCompilerResult.GenerateResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, errors);
            else
                instance = CompilerResult.GenerateResult(sourceFileName, targetFileName, isSuccess, result, errors);

            return instance;
        }
    }
}