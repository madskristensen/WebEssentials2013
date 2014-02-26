﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE80;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.EditorHelpers;

namespace MadsKristensen.EditorExtensions
{
    internal class CssExtractToFile : CommandTargetBase<ExtractCommandId>
    {
        private DTE2 _dte;
        private List<string> _possible = new List<string>() { ".CSS", ".LESS", ".JS", ".TS" };

        public CssExtractToFile(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, ExtractCommandId.ExtractSelection)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(ExtractCommandId commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (TextView == null)
                return false;

            string content = TextView.Selection.SelectedSpans[0].GetText();
            string extension = Path.GetExtension(_dte.ActiveDocument.FullName).ToLowerInvariant();

            if (!_possible.Contains(extension.ToUpperInvariant()))
            {
                extension = ".css";
            }

            string name = Interaction.InputBox("Specify the name of the file", "Web Essentials", "file1" + extension).Trim();

            if (!string.IsNullOrEmpty(name))
            {

                if (string.IsNullOrEmpty(Path.GetExtension(name)))
                {
                    name = name + extension;
                }

                string fileName = Path.Combine(Path.GetDirectoryName(_dte.ActiveDocument.FullName), name);

                if (!File.Exists(fileName))
                {
                    using (EditorExtensionsPackage.UndoContext("Extract to file..."))
                    {
                        using (StreamWriter writer = new StreamWriter(fileName, false, new UTF8Encoding(true)))
                        {
                            writer.Write(content);
                        }

                        ProjectHelpers.AddFileToActiveProject(fileName);
                        TextView.TextBuffer.Delete(TextView.Selection.SelectedSpans[0].Span);
                        _dte.ItemOperations.OpenFile(fileName);
                    }
                }
                else
                {
                    Logger.ShowMessage("The file already exists.");
                }
            }

            return true;
        }

        private static bool IsValidTextBuffer(IWpfTextView view)
        {
            return (ProjectionBufferHelper.MapToBuffer(view, "css", view.Caret.Position.BufferPosition)
                ?? ProjectionBufferHelper.MapToBuffer(view, "javascript", view.Caret.Position.BufferPosition)
                ) != null;
        }

        protected override bool IsEnabled()
        {
            var item = _dte.Solution.FindProjectItem(_dte.ActiveDocument.FullName);
            bool hasProject = item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName);

            if (hasProject && TextView != null && IsValidTextBuffer(TextView) && TextView.Selection.SelectedSpans.Count > 0)
            {
                return TextView.Selection.SelectedSpans[0].Length > 0;
            }

            return false;
        }
    }
}