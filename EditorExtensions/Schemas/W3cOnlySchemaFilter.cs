using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSS.Editor;
//using Microsoft.CSS.Editor;
using Microsoft.CSS.Core;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using System.IO;
using Microsoft.CSS.Editor.Schemas;

namespace MadsKristensen.EditorExtensions.Schemas
{
    [Export(typeof(ICssSchemaModuleProvider))]
    [Name("W3cOnlySchemaFilter")]
    [Order(Before = "User Schemas")]
    class W3cOnlySchemaFilter : ICssSchemaModuleProvider
    {
        private byte[] _emptyModule = Encoding.UTF8.GetBytes("<CssModule></CssModule>");
        public Stream GetModuleStream(string name)
        {
            if (WESettings.GetBoolean(WESettings.Keys.OnlyW3cAllowed) && name.Contains("vendor"))
            {
                return new MemoryStream(_emptyModule);
            }

            return null;
        }
    }
}
