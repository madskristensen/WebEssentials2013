
namespace MadsKristensen.EditorExtensions
{
    public class CompilerResult
    {
        public CompilerResult(string fileName)
        {
            FileName = fileName;
        }

        public bool IsSuccess { get; set; }
        public string FileName { get; set; }
        public string Result { get; set; }
        public CompilerError Error { get; set; }
    }
}