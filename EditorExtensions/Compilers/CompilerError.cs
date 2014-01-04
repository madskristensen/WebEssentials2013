
namespace MadsKristensen.EditorExtensions
{
    public class CompilerError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string FileName { get; set; }
        public string Message { get; set; }
    }
}
