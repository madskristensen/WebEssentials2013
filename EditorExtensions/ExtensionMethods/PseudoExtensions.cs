using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Schemas;

namespace MadsKristensen.EditorExtensions
{
    public static class PseudoExtensions
    {
        public static bool IsPseudoElement(this ParseItem item)
        {
            if (item.Text.StartsWith("::"))
                return true;

            var schema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);
            return schema.GetPseudo(":" + item.Text) != null;
        }
    }
}
