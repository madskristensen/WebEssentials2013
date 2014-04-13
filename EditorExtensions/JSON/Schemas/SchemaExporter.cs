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
            {".bowerrc", "http://vswebessentials.com/schemas/v0.1/bowerrc.json"},
            {"bower.json", "http://vswebessentials.com/schemas/v0.1/bower.json"},
            {"package.json", "http://vswebessentials.com/schemas/v0.1/package.json"},
            {"tslint.json", "http://vswebessentials.com/schemas/v0.1/tslint.json"},
            {"jscsrc.json", "http://vswebessentials.com/schemas/v0.1/jscsrc.json"},
            {".jscsrc", "http://vswebessentials.com/schemas/v0.1/jscsrc.json"},
            {"coffeelint.json", "http://vswebessentials.com/schemas/v0.1/coffeelint.json"},
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