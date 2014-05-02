using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.LiveScript
{
    /// <summary>
    /// Exports the LiveScript content type and file extension
    /// </summary>
    public class LiveScriptContentTypeDefinition
    {
        public const string LiveScriptContentType = "LiveScript";

        /// <summary>
        /// Exports the LiveScript content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(LiveScriptContentType)]
        [BaseDefinition("CoffeeScript")]
        public ContentTypeDefinition ILiveScriptContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(LiveScriptContentType)]
        [FileExtension(".ls")]
        public FileExtensionToContentTypeDefinition LsFileExtension { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(LiveScriptContentType)]
        [FileExtension(".livescript")]
        public FileExtensionToContentTypeDefinition LiveScriptFileExtension { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(LiveScriptContentType)]
        [FileExtension(".lsc")]
        public FileExtensionToContentTypeDefinition LscScriptFileExtension { get; set; }
    }
}
