﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class DocumentFactory
    {
        private static readonly ConcurrentDictionary<string, IDocument> DocumentLookup = new ConcurrentDictionary<string, IDocument>();

        private static Func<string, bool, IDocument> GetFactory(string fullPath)
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

        internal static IDocument GetDocument(string fullPath, bool createIfRequired = false)
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

            currentDocument = factory(fullPath, createIfRequired);

            if (currentDocument == null)
            {
                return null;
            }

            var doc = DocumentLookup.GetOrAdd(fileName, x => currentDocument);
            doc.Reparse();
            return doc;
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
                catch(ObjectDisposedException)
                {
                }
            }
        }
    }
}
