using Microsoft.CSS.Core;
using Microsoft.Less.Core;
using System.IO;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class LessDocument : DocumentBase
    {
        private LessDocument(string file, FileSystemEventHandler fileDeletedCallback)
            : base(file, fileDeletedCallback)
        {
        }

        protected override ICssParser GetParser()
        {
            return new LessParser();
        }

        internal static IDocument For(string fullPath, FileSystemEventHandler fileDeletedCallback = null)
        {
            return DocumentBase.For(fullPath, fileDeletedCallback, (f, c) => new LessDocument(f, c));
        }
    }
}
