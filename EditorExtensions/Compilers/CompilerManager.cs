using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using MarkdownSharp;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Composition;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions.Compilers
{

    ///<summary>A base class to run a compiler on arbitary project files and report the results.</summary>
    ///<remarks>
    /// This class uses the project system.  It
    /// is used for all compilations, including
    /// margins, build, and save.
    ///</remarks>
    abstract class CompilerManagerBase
    {
        private readonly ICollection<ICompilationConsumer> _listeners;
        public IContentType ContentType { get; private set; }
        public ICompilerInvocationSettings Settings { get; private set; }

        public CompilerManagerBase(IContentType contentType)
        {
            ContentType = contentType;

            _listeners = Mef.GetAllImports<ICompilationConsumer>(contentType);
            Settings = WESettings.Instance.ForContentType<ICompilerInvocationSettings>(contentType);
        }

        public Task<CompilerResult> CompileAsync(string sourcePath, bool save)
        {
            return save ? CompileToDefaultOutputAsync(sourcePath) : CompileInMemoryAsync(sourcePath);
        }
        public Task<CompilerResult> CompileInMemoryAsync(string sourcePath)
        {
            return CompileAsync(sourcePath, null);
        }
        public Task<CompilerResult> CompileToDefaultOutputAsync(string sourcePath)
        {
            var targetPath = Path.GetFullPath(GetTargetPath(sourcePath));
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            return CompileAsync(sourcePath, targetPath);
        }
        private string GetTargetPath(string sourcePath)
        {
            if (string.IsNullOrEmpty(Settings.OutputDirectory))
                return Path.ChangeExtension(sourcePath, TargetExtension);

            string compiledFileName = Path.GetFileName(Path.ChangeExtension(sourcePath, TargetExtension));
            string sourceDir = Path.GetDirectoryName(sourcePath);

            // If the output path is not project-relative, combine it directly.
            if (!Settings.OutputDirectory.StartsWith("~/", StringComparison.OrdinalIgnoreCase)
             && !Settings.OutputDirectory.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(sourceDir, Settings.OutputDirectory, compiledFileName);

            string rootDir = ProjectHelpers.GetRootFolder();

            if (string.IsNullOrEmpty(rootDir))
                // If no project is loaded, assume relative to file anyway
                rootDir = sourceDir;

            return Path.Combine(
                rootDir,
                Settings.OutputDirectory.TrimStart('~', '/'),
                compiledFileName
            );
        }

        ///<summary>Compiles the specified source file, notifying all <see cref="ICompilationConsumer"/>s.</summary>
        ///<param name="sourcePath">The path to the source file.</param>
        ///<param name="targetPath">The path to save the compiled output, or null to compile in-memory.</param>

        private async Task<CompilerResult> CompileAsync(string sourcePath, string targetPath)
        {
            if (!string.IsNullOrEmpty(targetPath))
                ProjectHelpers.CheckOutFileFromSourceControl(targetPath);   // TODO: Only if output changed?

            var result = await RunCompilerAsync(sourcePath, targetPath);

            if (!string.IsNullOrEmpty(targetPath))
                ProjectHelpers.AddFileToProject(sourcePath, targetPath);

            foreach (var listener in _listeners)
                listener.OnCompiled(result);
            return result;
        }

        public abstract string TargetExtension { get; }
        protected abstract Task<CompilerResult> RunCompilerAsync(string sourcePath, string targetPath);
    }

    ///<summary>Compiles files using <see cref="NodeExecutorBase"/> classes and reports the results.</summary>
    class NodeCompilerManager : CompilerManagerBase
    {
        public NodeCompilerManager(IContentType contentType) : base(contentType)
        {
            Compiler = Mef.GetImport<NodeExecutorBase>(contentType);
        }

        public NodeExecutorBase Compiler { get; private set; }

        public override string TargetExtension { get { return Compiler.TargetExtension; } }
        protected override async Task<CompilerResult> RunCompilerAsync(string sourcePath, string targetPath)
        {
            bool isTemp = false;
            if (string.IsNullOrEmpty(targetPath))
            {
                isTemp = true;
                targetPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Compiler.TargetExtension);
            }

            try
            {
                return await Compiler.CompileAsync(sourcePath, targetPath);
            }
            finally
            {
                if (isTemp)
                    File.Delete(targetPath);
            }
        }
    }

    class MarkdownCompilerManager : CompilerManagerBase
    {
        public MarkdownCompilerManager(IContentType contentType) : base(contentType) { }
        public override string TargetExtension { get { return ".html"; } }
        protected override Task<CompilerResult> RunCompilerAsync(string sourcePath, string targetPath)
        {
            var result = new Markdown(WESettings.Instance.Markdown).Transform(File.ReadAllText(sourcePath));
            if (!string.IsNullOrEmpty(targetPath))
                File.WriteAllText(targetPath, result, new UTF8Encoding(false));

            return Task.FromResult(new CompilerResult(sourcePath, targetPath) { IsSuccess = true, Result = result });
        }
    }
}
