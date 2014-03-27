using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    public class CompilerResult
    {
        public string SourceFileName { get; protected set; }
        public string TargetFileName { get; protected set; }
        public bool IsSuccess { get; protected set; }
        public IEnumerable<CompilerError> Errors { get; protected set; }

        public string Result { get; set; }

        protected CompilerResult(string sourceFileName, string targetFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
        {
            SourceFileName = sourceFileName;
            TargetFileName = targetFileName;
            Errors = errors ?? Enumerable.Empty<CompilerError>();
            IsSuccess = isSuccess;
            Result = result;
        }

        public static CompilerResult GenerateResult(string sourceFileName, string targetFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
        {
            return new CompilerResult(sourceFileName, targetFileName, isSuccess, result, errors);
        }


    }
}