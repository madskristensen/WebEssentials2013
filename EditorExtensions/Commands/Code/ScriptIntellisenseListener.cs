using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [ContentType("VisualBasic")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class ScriptIntellisense : IWpfTextViewCreationListener
    {
        private ITextDocument _document;

        public void TextViewCreated(IWpfTextView textView)
        {
            textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out _document);

            if (_document != null)
            {
                _document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        private void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            ITextDocument document = (ITextDocument)sender;

            if (document.TextBuffer == null || e.FileActionType != FileActionTypes.ContentSavedToDisk)
                return;

            Process(e.FilePath);
        }

        public static void Process(string filePath)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                var item = EditorExtensionsPackage.DTE.Solution.FindProjectItem(filePath);
                var list = ProcessFile(item);

                if (list != null)
                {
                    AddScript(filePath, ".js", list);
                    AddScript(filePath, ".d.ts", list);
                }

            }), DispatcherPriority.ApplicationIdle, null);
        }

        private static void AddScript(string filePath, string extension, List<IntellisenseObject> list)
        {
            string resultPath = filePath + extension;
            
            if (File.Exists(resultPath))
            {
                IntellisenseWriter writer = new IntellisenseWriter();
                writer.Write(list, resultPath);
                var item = MarginBase.AddFileToProject(filePath, resultPath);

                if (extension.Equals(".d.ts", StringComparison.OrdinalIgnoreCase))
                    item.Properties.Item("ItemType").Value = "TypeScriptCompile";
                else
                {                    
                    item.Properties.Item("ItemType").Value = "None";
                }
            }
        }

        private static List<IntellisenseObject> ProcessFile(ProjectItem item)
        {
            if (item.FileCodeModel == null)
                return null;

            List<IntellisenseObject> list = new List<IntellisenseObject>();

            foreach (CodeElement element in item.FileCodeModel.CodeElements)
            {
                if (element.Kind == vsCMElement.vsCMElementNamespace)
                {
                    CodeNamespace cn = (CodeNamespace)element;
                    foreach (CodeElement member in cn.Members)
                    {
                        if (member.Kind == vsCMElement.vsCMElementClass)
                        {
                            ProcessClass((CodeClass)member, list);
                        }
                    }
                }
                else if (element.Kind == vsCMElement.vsCMElementClass)
                {
                    ProcessClass((CodeClass)element, list);
                }
            }

            return list;
        }

        private static void ProcessClass(CodeClass cc, List<IntellisenseObject> list)
        {

            IntellisenseObject data = new IntellisenseObject();
            data.Name = cc.Name;
            data.FullName = cc.FullName;
            data.Properties = new List<IntellisenseProperty>();

            foreach (CodeProperty property in cc.Members)
            {
                var prop = new IntellisenseProperty()
                {
                    Name = GetName(property),
                    Type = property.Type.AsString,
                    Summary = GetSummary(property)
                };

                data.Properties.Add(prop);
            }

            if (data.Properties.Count > 0)
                list.Add(data);
        }

        private static string GetName(CodeProperty property)
        {
            foreach (CodeAttribute attr in property.Attributes)
            {
                if (attr.Name == "UIHint")
                {
                    return attr.Value.Trim('"');
                }
            }

            return property.Name;
        }

        private static string GetSummary(CodeProperty property)
        {
            int start = property.DocComment.IndexOf("<summary>", StringComparison.OrdinalIgnoreCase);
            if (start > -1)
            {
                start = start + 9;
                int end = property.DocComment.IndexOf("</summary>", start, StringComparison.OrdinalIgnoreCase);
                if (end > -1)
                    return property.DocComment.Substring(start, end - start).Trim();
            }

            return null;
        }
    }
}
