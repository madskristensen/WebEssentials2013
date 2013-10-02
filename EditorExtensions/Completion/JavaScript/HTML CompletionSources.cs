using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Intellisense;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace MadsKristensen.EditorExtensions
{
    class ElementsByTagNameCompletionSource : FunctionCompletionSource
    {
        static readonly ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphXmlItem, StandardGlyphItem.GlyphItemPublic);
        protected override string FunctionName { get { return "getElementsByTagName"; } }

        static ReadOnlyCollection<Tuple<string, string>> tagNames = TagCompletionProvider.GetListEntriesCache(includeUnstyled: true)
                                    .Select(c => Tuple.Create(c.DisplayText, c.Description))
                                    .Distinct()
                                    .OrderBy(s => s)
                                    .ToList()
                                    .AsReadOnly();

        public override IEnumerable<Completion> GetEntries(char quoteChar, SnapshotPoint caret)
        {
            return tagNames.Select(t => new Completion(
                quoteChar + t.Item1 + quoteChar,
                quoteChar + t.Item1 + quoteChar,
                t.Item2,
                _glyph,
                null
            ));
        }
    }

    abstract class CssNameCacheCompletionSource : FunctionCompletionSource
    {
        static readonly ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphXmlAttribute, StandardGlyphItem.GlyphItemPublic);
        readonly ICssNameCache _classNames;
        public CssNameCacheCompletionSource(ICssNameCache classNames) { _classNames = classNames; }
        protected override string FunctionName { get { return "getElementsByClassName"; } }
        protected abstract CssNameType NameType { get; }

        public override IEnumerable<Completion> GetEntries(char quoteChar, SnapshotPoint caret)
        {
            return _classNames.GetNames(new Uri(caret.Snapshot.TextBuffer.GetFileName()), caret, CssNameType.Class)
                .Select(s => s.Name)
                .Distinct()
                .OrderBy(s => s)
                .Select(s => new Completion(quoteChar + s + quoteChar, quoteChar + s + quoteChar, null, _glyph, null));
        }
    }

    class ElementsByClassNameCompletionSource : CssNameCacheCompletionSource
    {
        public ElementsByClassNameCompletionSource(ICssNameCache classNames) : base(classNames) { }
        protected override string FunctionName { get { return "getElementsByClassName"; } }
        protected override CssNameType NameType { get { return CssNameType.Class; } }
    }
    class ElementsByIdCompletionSource : CssNameCacheCompletionSource
    {
        public ElementsByIdCompletionSource(ICssNameCache classNames) : base(classNames) { }
        protected override string FunctionName { get { return "getElementById"; } }
        protected override CssNameType NameType { get { return CssNameType.Id; } }
    }
}
