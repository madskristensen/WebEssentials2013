using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.JSON.Core.Schema;

namespace MadsKristensen.EditorExtensions.JSONLD
{
    [Export(typeof(IJSONSchemaSelector))]
    internal class SchemaExporter : IJSONSchemaSelector
    {
        private static Dictionary<string, string> _schemas = new Dictionary<string, string>
        {
            {".jsonld", "http://schemastore.org/schemas/json-ld.json"},
        };

        public IEnumerable<string> GetAvailableSchemas()
        {
            return _schemas.Values;
        }

        public string GetSchemaFor(string fileLocation)
        {
            string extension = Path.GetExtension(fileLocation).ToLowerInvariant();

            if (_schemas.ContainsKey(extension))
                return _schemas[extension];

            return null;
        }
    }
}