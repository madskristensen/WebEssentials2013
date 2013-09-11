using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class DocumentFactory
    {
        private static readonly ConcurrentDictionary<string, IDocument> DocumentLookup = new ConcurrentDictionary<string, IDocument>();

        private static Func<string, FileSystemEventHandler, IDocument> GetFactory(string fullPath)
        {
            var extension = (Path.GetExtension(fullPath) ?? "").ToUpperInvariant();

            switch (extension)
            {
                case ".CSS":
                    return CssDocument.For;
                case ".LESS":
                    return LessDocument.For;
                default:
                    Logger.Log("No document factory could be found for file type: " + extension);
                    return null;
            }
        }

        internal static IDocument GetDocument(string fullPath, FileSystemEventHandler fileDeletedCallback = null)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            var fileName = fullPath.ToLowerInvariant();

            IDocument currentDocument;
            if (DocumentLookup.TryGetValue(fileName, out currentDocument))
            {
                return currentDocument;
            }

            var factory = GetFactory(fileName);

            if (factory == null)
            {
                return null;
            }

            currentDocument = factory(fullPath, fileDeletedCallback);

            if (currentDocument == null)
            {
                return null;
            }

            return DocumentLookup.AddOrUpdate(fileName, x => currentDocument, (x, e) => currentDocument);
        }

        public static IEnumerable<IDocument> AllDocuments
        {
            get { return DocumentLookup.Values; }
        }

        public static void Clear()
        {
            var currentValues = DocumentLookup.Values.ToList();
            DocumentLookup.Clear();

            foreach (var value in currentValues)
            {
                try
                {
                    value.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
