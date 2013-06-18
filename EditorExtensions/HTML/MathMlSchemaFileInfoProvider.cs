//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.IO;
//using Microsoft.VisualStudio.Utilities;
//using Microsoft.VisualStudio.Web.HTML.Schemas;

//namespace MadsKristensen.EditorExtensions
//{
//    [Export(typeof(IHtmlSchemaFileInfoProvider))]
//    [Name("MathML")]
//    [Order(Before = "Default")]
//    internal class MathMlSchemaFileInfoProvider : IHtmlSchemaFileInfoProvider
//    {
//        private const string _file = @"C:\Users\madsk\Documents\mathml.xsd";

//        public IEnumerable<SchemaFileInfo> GetSchemas(string defaultSchemaPath, string defaultRegistryPath)
//        {
//            if (!File.Exists(_file))
//                yield break;

//            SchemaFileInfo info = new SchemaFileInfo()
//            {
//                File = _file,
//                FriendlyName = "MathML",
//                Uri = "http://www.w3.org/1998/Math/MathML",
//                IsMobile = true,
//                IsNonBrowsable = true,
//            };

//            yield return info;
//        }
//    }
//}
