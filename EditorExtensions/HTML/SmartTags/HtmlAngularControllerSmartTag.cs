using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
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
    [Name("HtmlAngularController")]
    internal class HtmlAngularControllerSmartTagProvider : IHtmlSmartTagProvider
    {
        public IHtmlSmartTag TryCreateSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element, AttributeNode attribute, int caretPosition, HtmlPositionType positionType)
        {
            if (element.HasAttribute("ng-controller"))
            {
                return new HtmlAngularControllerSmartTag(textView, textBuffer, element);
            }

            return null;
        }
    }

    internal class HtmlAngularControllerSmartTag : HtmlSmartTag
    {
        public HtmlAngularControllerSmartTag(ITextView textView, ITextBuffer textBuffer, ElementNode element)
            : base(textView, textBuffer, element, HtmlSmartTagPosition.StartTag)
        {
        }

        protected override IEnumerable<ISmartTagAction> GetSmartTagActions(ITrackingSpan span)
        {
            return new ISmartTagAction[] { new FormatSelectionSmartTagAction(this) };
        }

        class FormatSelectionSmartTagAction : HtmlSmartTagAction
        {
            public FormatSelectionSmartTagAction(HtmlSmartTag htmlSmartTag) :
                base(htmlSmartTag, "Add new Angular controller")
            { }

            public async override void Invoke()
            {
                string value = HtmlSmartTag.Element.GetAttribute("ng-controller").Value;

                if (string.IsNullOrEmpty(value))
                    value = "myController";

                string folder = ProjectHelpers.GetProjectFolder(WebEssentialsPackage.DTE.ActiveDocument.FullName);
                string file;

                using (var dialog = new SaveFileDialog())
                {
                    dialog.FileName = value + ".js";
                    dialog.DefaultExt = ".js";
                    dialog.Filter = "JS files | *.js";
                    dialog.InitialDirectory = folder;

                    if (dialog.ShowDialog() != DialogResult.OK)
                        return;

                    file = dialog.FileName;
                }

                using (WebEssentialsPackage.UndoContext((this.DisplayText)))
                {
                    string script = GetScript(value);
                    await FileHelpers.WriteAllTextRetry(file, script);

                    ProjectHelpers.AddFileToActiveProject(file);
                    WebEssentialsPackage.DTE.ItemOperations.OpenFile(file);
                }
            }

            private static string GetScript(string value)
            {
                using (Stream stream = typeof(HtmlAngularControllerSmartTag).Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.Resources.Scripts.AngularController.js"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return string.Format(CultureInfo.CurrentCulture, reader.ReadToEnd(), value);
                }
            }
        }
    }
}
