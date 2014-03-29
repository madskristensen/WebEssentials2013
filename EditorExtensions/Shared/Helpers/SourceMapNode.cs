
namespace MadsKristensen.EditorExtensions
{
    public abstract class SourceMapNode
    {
        public string SourceFilePath { get; set; }
        public int GeneratedColumn { get; set; }
        public int OriginalColumn { get; set; }
        public int GeneratedLine { get; set; }
        public int OriginalLine { get; set; }

        public override bool Equals(object obj)
        {
            var node = (obj as SourceMapNode);

            return node.SourceFilePath == this.SourceFilePath &&
                   node.GeneratedColumn == this.GeneratedColumn &&
                   node.OriginalColumn == this.OriginalColumn &&
                   node.GeneratedLine == this.GeneratedLine &&
                   node.OriginalLine == this.OriginalLine;
        }

        public override int GetHashCode()
        {
            return this.SourceFilePath.GetHashCode() ^
                   this.GeneratedColumn ^
                   this.OriginalColumn ^
                   this.GeneratedLine ^
                   this.OriginalLine;
        }
    }
}