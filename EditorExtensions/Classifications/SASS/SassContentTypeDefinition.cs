using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    // TODO: Remove this when the SASS editor is included in VS.
    public class SassContentTypeDefinition
    {
        public const string SassContentType = "Sass";

        [Export(typeof(ContentTypeDefinition))]
        [Name(SassContentType)]
        [BaseDefinition("CSS")]
        public ContentTypeDefinition ISassContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(SassContentType)]
        [FileExtension(".scss")]
        public FileExtensionToContentTypeDefinition SassFileExtension { get; set; }
    }
}
