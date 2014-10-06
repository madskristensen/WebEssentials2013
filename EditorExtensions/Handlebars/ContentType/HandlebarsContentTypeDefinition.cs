using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Handlebars
{
    /// <summary>
    /// Exports the Handlebars content type and file extension
    /// </summary>
    public class HandlebarsContentTypeDefinition
    {
        public const string HandlebarsContentType = "handlebars";

        /// <summary>
        /// Exports the Handlebars HTML content type
        /// </summary>
        [Export(typeof(ContentTypeDefinition))]
        [Name(HandlebarsContentType)]
        [BaseDefinition("htmlx")]
        public ContentTypeDefinition IHandlebarsContentType { get; set; }

        // All of these extensions must also be registered in registry.pkgdef.
        // See https://twitter.com/Schabse/status/390280700043472896 and http://blogs.msdn.com/b/noahric/archive/2010/03/01/new-extension-css-is-less.aspx

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(HandlebarsContentType)]
        [FileExtension(".hbs")]
        public FileExtensionToContentTypeDefinition HbsFileExtension { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(HandlebarsContentType)]
        [FileExtension(".handlebars")]
        public FileExtensionToContentTypeDefinition HandlebarsFileExtension { get; set; }
    }
}
