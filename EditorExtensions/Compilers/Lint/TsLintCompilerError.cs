using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class TsLintCompilerError
    {
        public string name { get; set; }
        public string failure { get; set; }
        public StartPosition startPosition { get; set; }

        internal class StartPosition
        {
            public int line { get; set; }
            public int character { get; set; }
            public int position { get; set; }
        }

        public static IEnumerable<CompilerError> ToCompilerError(IEnumerable<TsLintCompilerError> rawErrors)
        {
            return rawErrors.Select(error => new CompilerError()
                                                {
                                                    Message = error.failure,
                                                    Column = error.startPosition.character,
                                                    FileName = error.name,
                                                    Line = error.startPosition.line
                                                });
        }
    }
}
