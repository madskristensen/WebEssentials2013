using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Microsoft.Html.Core;
using Microsoft.Html.Editor.SmartTags;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Html
{
    [Export(typeof(IHtmlSmartTagProvider))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [Order(Before = "Default")]
    [Name("ExtractToFileSmartTagProvider")]
    internal class ExtractToFileSmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            if ((element.IsStyleBlock() || element.IsJavaScriptBlock()) && element.InnerRange.Length > 5)
            {
                return new ExtractToFileSmartTag(textView, textBuffer, element);
            }

            return null;
        }
    }

    internal class ExtractToFileSmartTag : HtmlSmartTag
    {
        public ExtractToFileSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.StartTag)
        { }

        protected override IEnumerable<ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            yield return new ExtractToFileTagAction(this);
        }

        class ExtractToFileTagAction : HtmlSmartTagAction
        {
            public ExtractToFileTagAction(HtmlSmartTag htmlSmartTag) :
                base(htmlSmartTag, "Extract To File")
            { }

            public async override void Invoke()
            {
                string file;
                string root = ProjectHelpers.GetProjectFolder(WebEssentialsPackage.DTE.ActiveDocument.FullName);

                if (CanSaveFile(root, out file))
                {
                    await MakeChanges(root, file);
                }
            }

            private bool CanSaveFile(string folder, out string fileName)
            {
                string ext = this.HtmlSmartTag.Element.IsStyleBlock() ? "css" : "js";

                fileName = null;

                using (var dialog = new SaveFileDialog())
                {
                    dialog.FileName = "file." + ext;
                    dialog.DefaultExt = "." + ext;
                    dialog.Filter = ext.ToUpperInvariant() + " files | *." + ext;
                    dialog.InitialDirectory = folder;

                    if (dialog.ShowDialog() != DialogResult.OK)
                        return false;

                    fileName = dialog.FileName;
                }

                return true;
            }

            private async Task MakeChanges(string root, string fileName)
            {
                var element = this.HtmlSmartTag.Element;
                var textBuffer = this.HtmlSmartTag.TextBuffer;
                string text = textBuffer.CurrentSnapshot.GetText(element.InnerRange.Start, element.InnerRange.Length);

                string reference = GetReference(element, fileName, root);

                using (WebEssentialsPackage.UndoContext((this.DisplayText)))
                {
                    textBuffer.Replace(new Span(element.Start, element.Length), reference);
                    await FileHelpers.WriteAllTextRetry(fileName, text);
                    WebEssentialsPackage.DTE.ItemOperations.OpenFile(fileName);
                    ProjectHelpers.AddFileToActiveProject(fileName);
                }
            }

            private static string GetReference(ElementNode element, string fileName, string root)
            {
                string relative = FileHelpers.RelativePath(root, fileName);
                string reference = "<script src=\"/{0}\"></script>";

                if (element.IsStyleBlock())
                    reference = "<link rel=\"stylesheet\" href=\"/{0}\" />";

                return string.Format(CultureInfo.CurrentCulture, reference, HttpUtility.HtmlAttributeEncode(relative));
            }
        }
    }
}
