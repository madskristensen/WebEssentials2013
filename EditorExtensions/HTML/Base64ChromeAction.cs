//using System;
//using System.IO;
//using System.Text;
//using System.Xml;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Web.HTML.Chrome;

//namespace MadsKristensen.EditorExtensions
//{
//    [ChromeAction, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
//    internal class Base64ChromeAction : IChromeAction
//    {
//        string IChromeAction.Name { get { return "Convert to dataURI"; } }

//        void IChromeAction.Execute(IWpfTextView view, string tagName, int tagPosition)
//        {
//            string line = view.TextBuffer.CurrentSnapshot.GetText(tagPosition, view.TextBuffer.CurrentSnapshot.Length - tagPosition);
//            int length = line.IndexOf('>') + 1;

//            if (length > 0)
//            {
//                string element = line.Substring(0, length);
//                XmlNode img = ConvertToXml(element);

//                if (img != null && img.Attributes["src"] != null)
//                {
//                    XmlAttribute src = img.Attributes["src"];
//                    string dataUri = ConvertToDataUri(src);
                 
//                    if (!string.IsNullOrEmpty(dataUri))
//                    {
//                        src.InnerText = dataUri;
//                        view.TextBuffer.Replace(new Span(tagPosition, length), img.OuterXml);
//                    }
//                }
//            }
//        }

//        private static string ConvertToDataUri(XmlAttribute src)
//        {
//            string fileName = ProjectHelpers.ToAbsoluteFilePath(src.InnerText);
//            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
//            {
//                return FileHelpers.ConvertToBase64(fileName);
//            }

//            return null;
//        }

//        private static XmlNode ConvertToXml(string element)
//        {
//            StringBuilder sb = new StringBuilder();

//            using (XmlWriter writer = XmlWriter.Create(sb))
//            {
//                writer.WriteRaw(element);
//            }

//            XmlDocument doc = new XmlDocument();
//            doc.LoadXml(sb.ToString());

//            return doc.SelectSingleNode("//img");
//        }

//        bool IChromeAction.IsAvailable(IWpfTextView view, string tagName, int tagPosition)
//        {
//            return tagName.Equals("img", StringComparison.OrdinalIgnoreCase);
//        }
//    }
//}