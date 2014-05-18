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
            {".jshintrc", "http://schemastore.org/schemas/jshintrc.json"},
            {".bowerrc", "http://schemastore.org/schemas/bowerrc.json"},
            {"bower.json", "http://schemastore.org/schemas/bower.json"},
            {"package.json", "http://schemastore.org/schemas/package.json"},
            {"tslint.json", "http://schemastore.org/schemas/tslint.json"},
            {"jscsrc.json", "http://schemastore.org/schemas/jscsrc.json"},
            {".jscsrc", "http://schemastore.org/schemas/jscsrc.json"},
            {"coffeelint.json", "http://schemastore.org/schemas/coffeelint.json"},
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