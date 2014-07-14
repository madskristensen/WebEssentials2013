using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    public class CompilerResult
    {
        public string SourceFileName { get; protected set; }
        public string TargetFileName { get; protected set; }
        public string MapFileName { get; protected set; }
        public bool IsSuccess { get; protected set; }
        public bool HasSkipped { get; protected set; }
        public IEnumerable<CompilerError> Errors { get; protected set; }

        public string Result { get; private set; }
        public string ResultMap { get; private set; }

        protected CompilerResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasSkipped)
        {
            SourceFileName = sourceFileName;
            TargetFileName = targetFileName;
            MapFileName = mapFileName;
            Errors = errors ?? Enumerable.Empty<CompilerError>();
            IsSuccess = isSuccess;
            Result = result;
            ResultMap = resultMap;
            HasSkipped = hasSkipped;
        }

        public static CompilerResult GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasSkipped = false)
        {
            return new CompilerResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, hasSkipped);
        }

        internal static CompilerResult UpdateResult(CompilerResult result, string resultString)
        {
            result.Result = resultString;
            return result;
        }
    }
}
