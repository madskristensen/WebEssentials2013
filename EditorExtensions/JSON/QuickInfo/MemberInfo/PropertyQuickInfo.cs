using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Schema;
using Microsoft.JSON.Core.Schema.Adapters;
using Microsoft.JSON.Core.Schema.Completion;
using Microsoft.JSON.Editor;
using Microsoft.JSON.Editor.Schema.Def;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class PropertyQuickInfo : IQuickInfoSource
    {
        private ITextBuffer _buffer;
        private IJSONSchemaResolver _schemaResolver;

        public PropertyQuickInfo(ITextBuffer subjectBuffer, IJSONSchemaResolver schemaResolver)
        {
            _buffer = subjectBuffer;
            _schemaResolver = schemaResolver;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (session == null || qiContent == null || qiContent.Count > 0)
                return;

            // Map the trigger point down to our buffer.
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            var doc = JSONEditorDocument.FromTextBuffer(_buffer);
            JSONParseItem item = doc.JSONDocument.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            JSONMember member = item.FindType<JSONMember>();
            if (member == null || member.Name == null)
                return;

            IJSONSchema schema = _schemaResolver.DetermineSchemaForTextBuffer(_buffer);

            if (schema != null)
            {
                IJSONSchemaPropertyNameCompletionInfo info = Foo(schema, member);

                if (info != null && !string.IsNullOrEmpty(info.PropertyDocumentation))
                {
                    applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(item.Start, item.Length, SpanTrackingMode.EdgeNegative);
                    qiContent.Add(info.DisplayText + Environment.NewLine + info.PropertyDocumentation);
                }
            }
        }

        public IJSONSchemaPropertyNameCompletionInfo Foo(IJSONSchema schema, JSONMember member)
        {
            var adapter = (IJSONProperty)JSONParseItemAdapter.Create(member, schema);
            var owner = (JSONObjectAdapter)adapter.Parent;
            var info = owner.GetPropertyNamesCompletionInfo().FirstOrDefault(x => x.DisplayText == adapter.Name.Trim('"'));
            return info;
        }

        private bool m_isDisposed;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }
}