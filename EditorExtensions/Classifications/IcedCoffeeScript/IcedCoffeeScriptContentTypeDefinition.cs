using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Exports the Iced CoffeeScript content type and file extension
    /// </summary>
    public class IcedCoffeeScriptContentTypeDefinition
    {
        public const string IcedCoffeeScriptContentType = "IcedCoffeeScript";

        /// <summary>
        /// Exports the WebVTT HTML content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(IcedCoffeeScriptContentType)]
        [BaseDefinition("CoffeeScript")]
        public ContentTypeDefinition IIcedCoffeeScriptContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(IcedCoffeeScriptContentType)]
        [FileExtension(".iced")]
        public FileExtensionToContentTypeDefinition IcedCoffeeScriptFileExtension { get; set; }
    }
}
