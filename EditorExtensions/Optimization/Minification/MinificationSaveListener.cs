using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Commands;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Optimization.Minification
{
    [Export(typeof(IFileSaveListener))]
    [ContentType("HTMLX")]
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    class MinificationSaveListener : IFileSaveListener
    {
        public void FileSaved(IContentType contentType, string path)
        {
            throw new NotImplementedException();
        }
    }
}
