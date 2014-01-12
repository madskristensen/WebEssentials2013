using System.IO;

namespace MadsKristensen.EditorExtensions
{
    class CompressionResult
    {
        public CompressionResult(string originalFileName, string resultFileName)
        {
            FileInfo original = new FileInfo(originalFileName);
            FileInfo result = new FileInfo(resultFileName);

            OriginalFileName = original.FullName;
            OriginalFileSize = original.Length;
            ResultFileName = result.FullName;
            ResultFileSize = result.Length;
        }

        public long OriginalFileSize { get; set; }
        public string OriginalFileName { get; set; }
        public long ResultFileSize { get; set; }
        public string ResultFileName { get; set; }

        public long Saving
        {
            get { return OriginalFileSize - ResultFileSize; }
        }
    }
}