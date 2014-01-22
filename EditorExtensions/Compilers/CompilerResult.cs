using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    public class CompilerResult
    {
        public CompilerResult(string sourceFileName, string targetFileName)
        {
            SourceFileName = sourceFileName;
            TargetFileName = targetFileName;
            Errors = Enumerable.Empty<CompilerError>();
        }

        public string SourceFileName { get; set; }
        public string TargetFileName { get; set; }
        public bool IsSuccess { get; set; }
        public string Result { get; set; }
        public IEnumerable<CompilerError> Errors { get; set; }
    }
    public class CompilerError
    {
        public string FileName { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}