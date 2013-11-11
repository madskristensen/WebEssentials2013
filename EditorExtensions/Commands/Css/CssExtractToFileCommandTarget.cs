using EnvDTE80;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace MadsKristensen.EditorExtensions
{
    internal class CssExtractToFile : CommandTargetBase
    {
        private DTE2 _dte;
        private List<string> _possible = new List<string>() { ".CSS", ".LESS", ".JS", ".TS" };

        public CssExtractToFile(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidExtractCmdSet, PkgCmdIDList.ExtractSelection)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
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
                    _dte.UndoContext.Open("Extract to file...");

                    using (StreamWriter writer = new StreamWriter(fileName, false, new UTF8Encoding(true)))
                    {
                        writer.Write(content);
                    }

                    ProjectHelpers.AddFileToActiveProject(fileName);
                    TextView.TextBuffer.Delete(TextView.Selection.SelectedSpans[0].Span);
                    _dte.ItemOperations.OpenFile(fileName);

                    _dte.UndoContext.Close();
                }
                else
                {
                    MessageBox.Show("The file already exist", "Web Essentials", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            return true;
        }

        private bool IsValidTextBuffer(IWpfTextView view)
        {
            var projection = view.TextBuffer as IProjectionBuffer;

            if (projection != null)
            {
                var snapshotPoint = view.Caret.Position.BufferPosition;

                var buffers = projection.SourceBuffers.Where(s =>
                    s.ContentType.IsOfType("css") ||
                    s.ContentType.IsOfType("javascript"));

                foreach (ITextBuffer buffer in buffers)
                {
                    SnapshotPoint? point = view.BufferGraph.MapDownToBuffer(snapshotPoint, PointTrackingMode.Negative, buffer, PositionAffinity.Predecessor);

                    if (point.HasValue)
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
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