using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Composition;

namespace MadsKristensen.EditorExtensions
{
    static class Mef
    {
        private static ICompositionService CompositionService { get { return WebEditor.CompositionService; } }

        public static void SatisfyImportsOnce(object instance) { CompositionService.SatisfyImportsOnce(instance); }
        public static T GetImport<T>(IContentType contentType) where T : class
        {
            return new ContentTypeImportComposer<T>(CompositionService).GetImport(contentType);
        }

        public static ICollection<T> GetAllImports<T>(IContentType contentType) where T : class
        {
            return new ContentTypeImportComposer<T>(CompositionService).GetAll(contentType);
        }
    }
}
