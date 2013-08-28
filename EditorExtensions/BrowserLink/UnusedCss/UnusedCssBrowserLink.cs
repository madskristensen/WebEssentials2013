using Microsoft.VisualStudio.Web.BrowserLink;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    [Export(typeof(BrowserLinkExtensionFactory))]
    [BrowserLinkFactoryName("UnusedCss")] // Not needed in final version of VS2013
    public class UnusedCssBrowserLinkExtensionFactory : BrowserLinkExtensionFactory
    {
    }

    public class UnusedCssBrowserLink
    {
    }
}
