using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [ContentType("VisualBasic")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class ScriptIntellisenseListener : IWpfTextViewCreationListener
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        private ITextDocument _document;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out _document))
            {
                _document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        private void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            ITextDocument document = (ITextDocument)sender;

            if (document.TextBuffer == null || e.FileActionType != FileActionTypes.ContentSavedToDisk)
                return;

            ProcessAsync(e.FilePath).DontWait("Creating IntelliSense file(s) for " + e.FilePath);
        }
        public static Task<bool> ProcessAsync(string filePath)
        {
            if (!File.Exists(filePath + IntellisenseParser.Ext.JavaScript) && !File.Exists(filePath + IntellisenseParser.Ext.TypeScript))
                return Task.FromResult(false);

            return Dispatcher.CurrentDispatcher.InvokeAsync(new Func<bool>(() =>
            {
                var item = ProjectHelpers.GetProjectItem(filePath);

                if (item == null)
                    return false;

                List<IntellisenseObject> list = null;

                try
                {
                    list = IntellisenseParser.ProcessFile(item);
                }
                catch (Exception ex)
                {
                    Logger.Log("An error occurred while processing code in " + filePath + "\n" + ex
                             + "\n\nPlease report this bug at https://github.com/madskristensen/WebEssentials2013/issues, and include the source of the file.");
                }

                if (list == null)
                    return false;

                AddScript(filePath, IntellisenseParser.Ext.JavaScript, list);
                AddScript(filePath, IntellisenseParser.Ext.TypeScript, list);

                return true;
            }), DispatcherPriority.ApplicationIdle).Task;
        }


        private static void AddScript(string filePath, string extension, List<IntellisenseObject> list)
        {
            string resultPath = filePath + extension;

            if (!File.Exists(resultPath))
                return;

            IntellisenseWriter.Write(list, resultPath);

            var item = ProjectHelpers.AddFileToProject(filePath, resultPath);

            if (extension.Equals(IntellisenseParser.Ext.TypeScript, StringComparison.OrdinalIgnoreCase))
                item.Properties.Item("ItemType").Value = "TypeScriptCompile";
            else
            {
                item.Properties.Item("ItemType").Value = "None";
            }
        }
    }
}