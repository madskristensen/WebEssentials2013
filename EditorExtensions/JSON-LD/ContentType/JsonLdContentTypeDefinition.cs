using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.JSONLD
{
    public class JsonLdContentTypeDefinition
    {
        public const string JsonLdContentType = "JSON-LD";

        [Export(typeof(ContentTypeDefinition))]
        [Name(JsonLdContentType)]
        [BaseDefinition("json")]
        public ContentTypeDefinition IJsonLdContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(JsonLdContentType)]
        [FileExtension(".jsonld")]
        public FileExtensionToContentTypeDefinition JsonLdFileExtension { get; set; }
    }
}
