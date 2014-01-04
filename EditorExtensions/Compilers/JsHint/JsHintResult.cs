
namespace MadsKristensen.EditorExtensions
{
    // Used for JSON serialization
    public class JsHintResult
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}
