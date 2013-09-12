using System.IO;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CssDocument : DocumentBase
    {
        private CssDocument(string file, FileSystemEventHandler fileDeletedCallback)
            : base(file, fileDeletedCallback)
        {
        }

        protected override ICssParser GetParser()
        {
            return new CssParser();
        }

        internal static IDocument For(string fullPath, FileSystemEventHandler fileDeletedCallback = null)
        {
            return For(fullPath, fileDeletedCallback, (f, c) => new CssDocument(f, c));
        }
    }
}