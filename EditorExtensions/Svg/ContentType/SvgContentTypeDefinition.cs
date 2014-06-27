using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Svg
{
    public class SvgContentTypeDefinition
    {
        public const string SvgContentType = "svg";

        /// <summary>
        /// Exports the Svg HTML content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(SvgContentType)]
        [BaseDefinition("htmlx")]
        public ContentTypeDefinition ISvgContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(SvgContentType)]
        [FileExtension(".svg")]
        public FileExtensionToContentTypeDefinition SvgFileExtension { get; set; }
    }
}
