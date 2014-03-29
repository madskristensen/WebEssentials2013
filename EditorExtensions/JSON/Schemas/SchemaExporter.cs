using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.JSON.Core.Schema;

namespace MadsKristensen.EditorExtensions.JSON
{
    [Export(typeof(IJSONSchemaSelector))]
    internal class SchemaExporter : IJSONSchemaSelector
    {
        private static Dictionary<string, string> _schemas = new Dictionary<string, string> 
        { 
            {".jshintrc", "http://vswebessentials.com/schemas/v0.1/jshintrc.json"},
        };

        public IEnumerable<string> GetAvailableSchemas()
        {
            return _schemas.Values;
        }

        public string GetSchemaFor(string fileLocation)
        {
            string fileName = Path.GetFileName(fileLocation).ToLowerInvariant();

            if (_schemas.ContainsKey(fileName))
                return _schemas[fileName];

            return null;
        }
    }
}