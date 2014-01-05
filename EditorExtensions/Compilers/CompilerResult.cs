using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    public class CompilerResult
    {
        public CompilerResult(string fileName)
        {
            FileName = fileName;
            Errors = Enumerable.Empty<CompilerError>();
        }

        public bool IsSuccess { get; set; }
        public string FileName { get; set; }
        public string Result { get; set; }
        public IEnumerable<CompilerError> Errors { get; set; }
    }
}