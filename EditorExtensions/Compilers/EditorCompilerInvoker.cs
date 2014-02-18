﻿using System;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions.Compilers
{
    ///<summary>Compiles an open document in the editor, updating the results when the file is saved.</summary>
    ///<remarks>
    /// This is only run when the file is opened in an editor; 
    /// it is not called on build.  All compilation logic that
    /// must run on build too should go in CompilerRunnerBase,
    /// or in an ICompilationConsumer.
    ///</remarks>
    class EditorCompilerInvoker : ICompilationNotifier
    {
        public ITextDocument Document { get; private set; }
        public CompilerRunnerBase CompilerRunner { get; private set; }

        public EditorCompilerInvoker(ITextDocument doc, CompilerRunnerBase compilerRunner)
        {
            Document = doc;
            CompilerRunner = compilerRunner;

            Document.FileActionOccurred += Document_FileActionOccurred;
        }

        ///<summary>Releases all resources used by the EditorCompilerInvoker.</summary>
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        ///<summary>Releases the unmanaged resources used by the EditorCompilerInvoker and optionally releases the managed resources.</summary>
        ///<param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Document.FileActionOccurred -= Document_FileActionOccurred;
            }
        }

        private void Document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk || e.FileActionType == FileActionTypes.DocumentRenamed)
                CompileAsync(e.FilePath).DontWait("compiling " + e.FilePath);
        }

        ///<summary>Occurs when the file has been compiled (on both success and failure).</summary>
        public event EventHandler<CompilerResultEventArgs> CompilationReady;

        ///<summary>Raises the CompilationReady event.</summary>
        ///<param name="e">A CompilerResultEventArgs object that provides the event data.</param>
        protected virtual void OnCompilationReady(CompilerResultEventArgs e)
        {
            if (CompilationReady != null)
                CompilationReady(this, e);
        }

        protected virtual Task CompileAsync(string sourcePath)
        {
            Logger.Log(CompilerRunner.SourceContentType + ": Compiling " + Path.GetFileName(sourcePath));
            return InitiateCompilationAsync(sourcePath, save: CompilerRunner.Settings.CompileOnSave).HandleErrors("compiling " + sourcePath);
        }

        public void RequestCompilationResult(bool cached)
        {
            if (cached && CompilerRunner.Settings.CompileOnSave)
            {
                var targetPath = CompilerRunner.GetTargetPath(Document.FilePath);
                if (File.Exists(targetPath))
                {
                    OnCompilationReady(new CompilerResultEventArgs(new CompilerResult(Document.FilePath, targetPath)
                    {
                        IsSuccess = true,
                        Result = File.ReadAllText(targetPath)
                    }));

                    return;
                }
            }
            InitiateCompilationAsync(Document.FilePath, save: false).DontWait("compiling " + Document.FilePath);
        }
        async Task InitiateCompilationAsync(string sourcePath, bool save)
        {
            var result = await CompilerRunner.CompileAsync(sourcePath, save);
            OnCompilationReady(new CompilerResultEventArgs(result));
        }
    }

    ///<summary>An <see cref="EditorCompilerInvoker"/> that reports compilation errors to the error list.</summary>
    class ErrorReportingCompilerInvoker : EditorCompilerInvoker
    {
        private readonly ErrorListProvider _provider;


        public ErrorReportingCompilerInvoker(ITextDocument doc, CompilerRunnerBase compilerRunner)
            : base(doc, compilerRunner)
        {
            _provider = new ErrorListProvider(EditorExtensionsPackage.Instance);
        }

        ///<summary>Releases the unmanaged resources used by the ErrorReportingCompilerInvoker and optionally releases the managed resources.</summary>
        ///<param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _provider.Dispose();
            }
            base.Dispose(disposing);
        }
        protected override Task CompileAsync(string sourcePath)
        {
            _provider.Tasks.Clear();
            return base.CompileAsync(sourcePath);
        }
        protected override void OnCompilationReady(CompilerResultEventArgs e)
        {
            foreach (var error in e.CompilerResult.Errors)
                CreateTask(error);
            base.OnCompilationReady(e);
        }

        private void CreateTask(CompilerError error)
        {
            ErrorTask task = new ErrorTask()
            {
                Line = error.Line,
                Column = error.Column,
                ErrorCategory = TaskErrorCategory.Error,
                Category = TaskCategory.Html,
                Document = error.FileName,
                Priority = TaskPriority.Low,
                Text = error.Message,
            };

            task.AddHierarchyItem();

            task.Navigate += task_Navigate;
            _provider.Tasks.Add(task);
        }

        private void task_Navigate(object sender, EventArgs e)
        {
            ErrorTask task = sender as ErrorTask;

            _provider.Navigate(task, new Guid(Constants.vsViewKindPrimary));

            if (task.Column > 0)
            {
                var doc = (TextDocument)EditorExtensionsPackage.DTE.ActiveDocument.Object("textdocument");
                doc.Selection.MoveToLineAndOffset(task.Line, task.Column, false);
            }
        }
    }
}
