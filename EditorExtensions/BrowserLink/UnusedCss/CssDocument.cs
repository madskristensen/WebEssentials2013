using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSS.Core;
using System.Threading;
using System.Collections.Concurrent;

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
            return DocumentBase.For(fullPath, fileDeletedCallback, (f, c) => new CssDocument(f, c));
        }
    }
}