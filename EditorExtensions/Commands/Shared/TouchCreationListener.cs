using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class TouchCreationListener : IVsTextViewCreationListener
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            view.VisualElement.TouchMove += VisualElement_TouchMove;
        }

        void VisualElement_TouchMove(object sender, System.Windows.Input.TouchEventArgs e)
        {
            //var input = (System.Windows.IInputElement)sender;

            //System.Windows.Forms.MessageBox.Show(e.GetTouchPoint(input).Action);
        }

    }
}
