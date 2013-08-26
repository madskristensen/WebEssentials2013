using Microsoft.VisualStudio.Web.BrowserLink;
using System.ComponentModel.Composition;
using System.IO;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    [Export(typeof(BrowserLinkExtensionFactory))]
    [BrowserLinkFactoryName("UnusedCss")]
    public class UnusedCssExtensionFactory : BrowserLinkExtensionFactory
    {
        public override BrowserLinkExtension CreateInstance(BrowserLinkConnection connection)
        {
            return new UnusedCssExtension(connection);
        }

        public override string Script
        {
            get
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
}