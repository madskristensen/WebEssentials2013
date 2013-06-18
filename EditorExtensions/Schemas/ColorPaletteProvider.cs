using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;

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
                return Path.Combine(folder, "schemas\\css-we-settings.xml");
            }
        }
    }
}
