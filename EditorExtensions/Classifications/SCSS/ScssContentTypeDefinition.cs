//using Microsoft.VisualStudio.Utilities;
//using System.ComponentModel.Composition;

//namespace MadsKristensen.EditorExtensions
//{
//    /// <summary>
//    /// Exports the ScSS content type and file extension
//    /// </summary>
//    public class ScssContentTypeDefinition
//    {
//        public const string ScssLanguageName = "scss";
//        public const string ScssContentType = "scss";
//        public const string ScssFileExtension = ".scss";

//        /// <summary>
//        /// Exports the SaSS CSS content type
//        /// </summary>
//        [Export(typeof(ContentTypeDefinition))]
//        [Name(ScssContentType)]
//        [BaseDefinition("LESS")]
//        public ContentTypeDefinition IScssContentType { get; set; }

//        /// <summary>
//        /// Exports the SaSS file extension
//        /// </summary>
//        [Export(typeof(FileExtensionToContentTypeDefinition))]
//        [ContentType(ScssContentType)]
//        [FileExtension(ScssFileExtension)]
//        public FileExtensionToContentTypeDefinition IScssFileExtension { get; set; }
//    }
//}
