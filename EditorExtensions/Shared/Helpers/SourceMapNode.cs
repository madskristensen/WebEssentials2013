
namespace MadsKristensen.EditorExtensions
{
    public abstract class SourceMapNode
    {
        public string SourceFilePath { get; set; }
        public int GeneratedColumn { get; set; }
        public int GeneratedLine { get; set; }
        public int OriginalColumn { get; set; }
        public int OriginalLine { get; set; }
    }
}