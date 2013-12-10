using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Schemas
{
    [Export(typeof(ICssSchemaModuleProvider))]
    [Name("BrowserSchemaFilter")]
    [Order(Before = "User Schemas")]
    class BrowserSchemaFilter : ICssSchemaModuleProvider
    {
        private byte[] _emptyModule = Encoding.UTF8.GetBytes("<CssModule></CssModule>");

        public Stream GetModuleStream(string name)
        {
            if (name.Contains("vendor") && BrowserStore.Browsers.Count > 0)
            {
                if (!BrowserStore.Browsers.Contains("IE") && name.Contains("-ms."))
                {
                    return new MemoryStream(_emptyModule);
                }

                if (!BrowserStore.Browsers.Contains("FF") && name.Contains("-moz."))
                {
                    return new MemoryStream(_emptyModule);
                }

                if (!BrowserStore.Browsers.Contains("O") && name.Contains("-o."))
                {
                    return new MemoryStream(_emptyModule);
                }

                if (!BrowserStore.Browsers.Contains("C") && !BrowserStore.Browsers.Contains("S") && name.Contains("-webkit."))
                {
                    return new MemoryStream(_emptyModule);
                }
            }

            return null;
        }
    }
}
