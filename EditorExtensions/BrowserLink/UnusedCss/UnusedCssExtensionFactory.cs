using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    [Export(typeof(IBrowserLinkExtensionFactory))]
    public class UnusedCssExtensionFactory : IBrowserLinkExtensionFactory
    {
        public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
        {
            return new UnusedCssExtension(connection);
        }

        public string GetScript()
        {
            using (var stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.UnusedCss.UnusedCss.js"))
            {
                if (stream == null)
                {
                    Logger.Log("Could not get script for extension " + typeof(UnusedCssExtension));

                    return "";
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}