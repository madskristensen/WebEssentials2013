using System.ComponentModel.Composition;
using Microsoft.JSON.Editor.Schema.Def;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Property QuickInfo Source")]
    [Order(Before = "Default Quick Info Presenter")]
    [ContentType("JSON")]
    internal class PropertyQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        [Import]
        public IJSONSchemaResolver SchemaResolver { get; set; }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new PropertyQuickInfo(textBuffer, SchemaResolver);
        }
    }
}
