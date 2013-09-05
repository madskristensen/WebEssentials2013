using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class DocumentFactory
    {
        private static Func<string, FileSystemEventHandler, IDocument> GetFactory(string fullPath)
        {
            var extension = Path.GetExtension(fullPath).ToUpperInvariant();

            switch (extension)
            {
                case ".CSS":
                    return CssDocument.For;
                //case ".LESS":
                //    return LessDocument.For;
                default:
                    Logger.Log("No document factory could be found for file type: " + extension);
                    return null;
            }
        }

        internal static IDocument GetDocument(string fullPath, FileSystemEventHandler fileDeletedCallback = null)
        {
            var fileName = fullPath.ToLowerInvariant();
            var factory = GetFactory(fileName);

            if (factory == null)
            {
                return null;
            }

            return factory(fullPath, fileDeletedCallback);
        }
    }
}
