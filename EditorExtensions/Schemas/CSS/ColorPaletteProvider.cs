using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssSchemaFileProvider))]
    [Name("ColorPaletteProvider")]
    internal class ColorPaletteProvider : ICssSchemaFileProvider
    {
        public string File
        {
            get
            {
                string assembly = Assembly.GetExecutingAssembly().Location;
                string folder = Path.GetDirectoryName(assembly);
                return Path.Combine(folder, "schemas\\css\\css-we-settings.xml");
            }
        }
    }
}
