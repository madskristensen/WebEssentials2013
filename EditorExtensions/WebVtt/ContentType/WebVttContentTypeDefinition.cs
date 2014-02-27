using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.WebVtt
{
    public class WebVttContentTypeDefinition
    {
        public const string WebVttContentType = "webvtt";

        /// <summary>
        /// Exports the WebVTT HTML content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(WebVttContentType)]
        [BaseDefinition("plaintext")]
        public ContentTypeDefinition IWebVttContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(WebVttContentType)]
        [FileExtension(".vtt")]
        public FileExtensionToContentTypeDefinition WebVttFileExtension { get; set; }
    }
}
