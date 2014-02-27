using System;
using System.Collections.Generic;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class RemoveCssSignatureHelpSource : ISignatureHelpSource
    {
        private ITextBuffer _buffer;

        public RemoveCssSignatureHelpSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
        {
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            CssEditorDocument document = CssEditorDocument.FromTextBuffer(_buffer);
            ParseItem item = document.StyleSheet.ItemBeforePosition(point.Value.Position);

            if (item == null)
                return;

            Declaration dec = item.FindType<Declaration>();
            if (dec == null || dec.PropertyName == null || dec.Colon == null)
                return;

            foreach (ISignature signature in signatures)
            {
                if (signature is ValueOrderSignature)
                {
                    signatures.RemoveAt(signatures.Count - 1);
                    break;
                }
            }
        }

        public ISignature GetBestMatch(ISignatureHelpSession session)
        {
            return (session.Signatures != null && session.Signatures.Count > 0)
                ? session.Signatures[0]
                : null;
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
