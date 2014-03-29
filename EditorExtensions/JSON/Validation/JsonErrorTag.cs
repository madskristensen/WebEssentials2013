using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Validation;

namespace MadsKristensen.EditorExtensions.JSON
{
    class JsonErrorTag : IJSONError
    {
        public JSONErrorFlags Flags { get; set; }

        public JSONParseItem Item { get; set; }

        public string Text { get; set; }

        public int AfterEnd { get; set; }

        public int Length { get; set; }

        public int Start { get; set; }
    }
}
