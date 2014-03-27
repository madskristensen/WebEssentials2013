using System.Collections.Generic;
using System.IO;

namespace MadsKristensen.EditorExtensions
{
    public class CssCompilerResult : CompilerResult
    {
        public CssSourceMap SourceMap { get; protected set; }

        private CssCompilerResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
            : base(sourceFileName, targetFileName, isSuccess, result, errors)
        {
            var extension = Path.GetExtension(sourceFileName).TrimStart('.');
            SourceMap = new CssSourceMap(targetFileName, mapFileName, Mef.GetContentType(extension));
        }

        public static CssCompilerResult GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
        {
            return new CssCompilerResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, errors);
        }
    }
}