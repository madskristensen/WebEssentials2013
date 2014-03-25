using System;
using System.Collections.Generic;
using MadsKristensen.EditorExtensions.Compilers;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Less.Core;
using Microsoft.Scss.Core;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class SelectorQuickInfo : IQuickInfoSource
    {
        private ITextBuffer _buffer;
        private ITextDocumentFactoryService _documentFactory;

        public SelectorQuickInfo(ITextBuffer subjectBuffer, ITextDocumentFactoryService documentFactory)
        {
            _buffer = subjectBuffer;
            _documentFactory = documentFactory;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (session == null || qiContent == null)
                return;

            // Map the trigger point down to our buffer.
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (!point.HasValue)
                return;

            var tree = CssEditorDocument.FromTextBuffer(_buffer);
            ParseItem item = tree.StyleSheet.ItemBeforePosition(point.Value.Position);

            if (item == null || !item.IsValid)
                return;

            Selector sel = item.FindType<Selector>();

            if (sel == null)
                return;

            // Mixins don't have specificity
            if (sel.SimpleSelectors.Count == 1)
            {
                var subSelectors = sel.SimpleSelectors[0].SubSelectors;

                if (subSelectors.Count == 1 &&
                    subSelectors[0] is LessMixinDeclaration &&
                    subSelectors[0] is ScssMixinDeclaration)
                    return;
            }

            applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(item.Start, item.Length, SpanTrackingMode.EdgeNegative);

            if (_buffer.ContentType.DisplayName.Equals("css", StringComparison.OrdinalIgnoreCase))
            {
                qiContent.Add(GenerateContent(sel));
                return;
            }

            HandlePreprocessor(session, point, item, tree, qiContent);
        }

        private void HandlePreprocessor(IQuickInfoSession session, SnapshotPoint? point, ParseItem item, CssEditorDocument tree, IList<object> qiContent)
        {
            ITextDocument document;

            if (!_documentFactory.TryGetTextDocument(session.TextView.TextDataModel.DocumentBuffer, out document))
                return;

            var notifierProvider = Mef.GetImport<ICompilationNotifierProvider>(_buffer.ContentType);
            var notifier = notifierProvider.GetCompilationNotifier(document);
            var result = WebEditor.ExportProvider.GetExport<ITextBufferFactoryService>().Value.CreateTextBuffer();
            var line = point.Value.GetContainingLine().LineNumber;

            qiContent.Add(result);

            notifier.CompilationReady += async (s, e) =>
            {
                var selector = await CssSourceMap.GetGeneratedSelectorAsync(_buffer.GetFileName(), e.CompilerResult.TargetFileName, line, tree.StyleSheet.Text.GetLineColumn(item.Start, line));

                if (selector == null)
                    return;

                result.SetText(GenerateContent(selector));
            };

            notifier.RequestCompilationResult(true);
        }

        private static string GenerateContent(Selector sel)
        {
            SelectorSpecificity specificity = new SelectorSpecificity(sel);

            return "Selector specificity:\t\t" + specificity.ToString().Trim();

            //sb.AppendLine(" - IDs:\t\t\t\t" + specificity.IDs);
            //sb.AppendLine(" - Classes:\t\t\t" + (specificity.Classes + specificity.PseudoClasses));
            //sb.AppendLine(" - Attributes:\t\t" + specificity.Attributes);
            //sb.AppendLine(" - Elements:\t\t" + (specificity.Elements + specificity.PseudoElements));
        }

        public void Dispose() { }
    }
}
