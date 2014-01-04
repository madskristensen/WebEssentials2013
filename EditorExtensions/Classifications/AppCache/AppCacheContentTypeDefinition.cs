using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    public class AppCacheContentTypeDefinition
    {
        public const string AppCacheContentType = "AppCache";

        /// <summary>
        /// Exports the AppCache HTML content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(AppCacheContentType)]
        [BaseDefinition("plaintext")]
        public ContentTypeDefinition IAppCacheContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(AppCacheContentType)]
        [FileExtension(".appcache")]
        public FileExtensionToContentTypeDefinition AppCacheFileExtension { get; set; }
    }
}
