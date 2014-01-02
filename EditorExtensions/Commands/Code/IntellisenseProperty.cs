using System.Diagnostics.CodeAnalysis;

namespace MadsKristensen.EditorExtensions
{
    public class IntellisenseProperty
    {
        public string Name { get; set; }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods",
            Justification = "Unambiguous in this context.")]
        public IntellisenseType Type { get; set; }

        public string Summary { get; set; }
    }
}