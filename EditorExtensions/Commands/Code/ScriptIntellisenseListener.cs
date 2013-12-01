using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [ContentType("VisualBasic")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class ScriptIntellisense : IWpfTextViewCreationListener
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

            Process(e.FilePath);
        }

        public static void Process(string filePath)
        {
            if (!File.Exists(filePath + ".js") && !File.Exists(filePath + ".d.ts"))
                return;

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

            if (!File.Exists(resultPath))
                return;

            IntellisenseWriter.Write(list, resultPath);
            var item = MarginBase.AddFileToProject(filePath, resultPath);

            if (extension.Equals(".d.ts", StringComparison.OrdinalIgnoreCase))
                item.Properties.Item("ItemType").Value = "TypeScriptCompile";
            else
            {
                item.Properties.Item("ItemType").Value = "None";
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
            IntellisenseObject data = new IntellisenseObject
            {
                Name = cc.Name,
                FullName = cc.FullName,
                Properties = new List<IntellisenseProperty>()
            };

            foreach (CodeProperty property in cc.Members.OfType<CodeProperty>())
            {
                if (property.Attributes.Cast<CodeAttribute>().Any(a => a.Name == "IgnoreDataMember"))
                    continue;

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
                if (attr.Name != "DataMember" && !attr.Name.EndsWith(".DataMember"))
                    continue;

                var value = attr.Children.OfType<CodeAttributeArgument>().FirstOrDefault(p => p.Name == "Name");

                if (value == null)
                    break;
                // Strip the leading & trailing quotes
                return value.Value.Substring(1, value.Value.Length - 2);
            }

            return property.Name;
        }

        private static string GetSummary(CodeProperty property)
        {
            if (string.IsNullOrWhiteSpace(property.DocComment))
                return null;

            try
            {
                return XElement.Parse(property.DocComment)
                               .Descendants("summary")
                               .Select(x => x.Value)
                               .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Logger.Log("Couldn't parse XML Doc Comment for " + property.FullName + ":\n" + ex);
                return null;
            }
        }
    }
}
