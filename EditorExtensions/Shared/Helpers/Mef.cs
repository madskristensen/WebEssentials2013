using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Composition;

namespace MadsKristensen.EditorExtensions
{
    static class Mef
    {
        private static ICompositionService CompositionService { get { return WebEditor.CompositionService; } }

        public static void SatisfyImportsOnce(object instance) { CompositionService.SatisfyImportsOnce(instance); }

        ///<summary>Gets all ContentTypes that export a given service.  Does not return derived content types.</summary>
        public static IEnumerable<IContentType> GetSupportedContentTypes<T>()
        {
            var ctr = WebEditor.ExportProvider.GetExport<IContentTypeRegistryService>();

            return WebEditor.ExportProvider.GetExports<T, IContentTypeMetadata>()
                            .SelectMany(o => o.Metadata.ContentTypes)
                            .Select(ctr.Value.GetContentType)
                            .Where(e => e != null);
        }

        public static IContentType GetContentType(string name)
        {
            var ctr = WebEditor.ExportProvider.GetExport<IContentTypeRegistryService>().Value;

            return ctr.GetContentType(name);
        }

        ///<summary>Gets all extensions (including leading dot) that export a given service.  Does not return derived content types.</summary>
        public static IEnumerable<string> GetSupportedExtensions<T>()
        {
            var fers = WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>();

            return GetSupportedContentTypes<T>()
                  .SelectMany(fers.Value.GetExtensionsForContentType)
                  .Select(e => "." + e);
        }

        public static IEnumerable<IContentType> GetChainCompilationContentTypes()
        {
            return new[] {
                ContentTypeManager.GetContentType("LESS"),
                ContentTypeManager.GetContentType("SCSS")
            };
        }

        public static ISet<string> GetChainedCompileExtensions()
        {
            var fers = WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>();

            return new HashSet<string>(GetChainCompilationContentTypes()
                    .SelectMany(fers.Value.GetExtensionsForContentType)
                    .Select(e => "*." + e));
        }

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
