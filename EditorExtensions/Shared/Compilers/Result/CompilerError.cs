
namespace MadsKristensen.EditorExtensions
{
    public class CompilerError
    {
        public string FileName { get; set; }
        public string Message { get; set; }
        public string FullMessage { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}