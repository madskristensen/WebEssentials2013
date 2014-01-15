using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Commands;
using MarkdownSharp;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions.Compilers
{
    ///<summary>A base class to run a compiler on arbitrary project files and report the results.</summary>
    ///<remarks>
    /// This class uses the project system.  It
    /// is used for all compilations, including
    /// margins, build, and save.
    ///</remarks>
    public abstract class CompilerRunnerBase
    {
        private readonly ICollection<IFileSaveListener> _listeners;
        public abstract string TargetExtension { get; }
        public IContentType SourceContentType { get; private set; }
        public IContentType TargetContentType { get; private set; }
        public ICompilerInvocationSettings Settings { get; private set; }

        public IFileExtensionRegistryService FileExtensionRegistry { get; set; }
        public CompilerRunnerBase(IContentType contentType)
        {
            Mef.SatisfyImportsOnce(this);
            SourceContentType = contentType;
            TargetContentType = FileExtensionRegistry.GetContentTypeForExtension(TargetExtension.TrimEnd('.'));

            _listeners = Mef.GetAllImports<IFileSaveListener>(contentType);
            Settings = WESettings.Instance.ForContentType<ICompilerInvocationSettings>(contentType);
        }

        ///<summary>Compiles a source file, optionally saving it to the default output directory.</summary>
        /// <param name="sourcePath">The source file to compile.</param>
        /// <param name="save">True to save the compiled file(s) to the default output directory.</param>
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
        ///<summary>Gets the default save location for the compiled results of the specified file, based on user settings.</summary>
        public string GetTargetPath(string sourcePath)
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
        public async Task<CompilerResult> CompileAsync(string sourcePath, string targetPath)
        {
            if (!string.IsNullOrEmpty(targetPath))
                ProjectHelpers.CheckOutFileFromSourceControl(targetPath);   // TODO: Only if output changed?

            var result = await RunCompilerAsync(sourcePath, targetPath);

            if (result != null && result.IsSuccess && !string.IsNullOrEmpty(targetPath))
            {
                ProjectHelpers.AddFileToProject(sourcePath, targetPath);
                foreach (var listener in _listeners)
                    listener.FileSaved(TargetContentType, result.TargetFileName);
            }
            return result;
        }

        protected abstract Task<CompilerResult> RunCompilerAsync(string sourcePath, string targetPath);
    }

    [Export(typeof(ICompilerRunnerProvider))]
    [ContentType("LESS")]
    [ContentType("SASS")]
    [ContentType("CoffeeScript")]
    [ContentType("IcedCoffeeScript")]
    public class NodeCompilerRunnerProvider : ICompilerRunnerProvider
    {
        public CompilerRunnerBase GetCompiler(IContentType contentType) { return new NodeCompilerRunner(contentType); }
    }

    ///<summary>Compiles files using <see cref="NodeExecutorBase"/> classes and reports the results.</summary>
    class NodeCompilerRunner : CompilerRunnerBase
    {
        public NodeCompilerRunner(IContentType contentType) : base(contentType)
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

    [Export(typeof(ICompilerRunnerProvider))]
    [ContentType("Markdown")]
    public class MarkdownCompilerRunnerProvider : ICompilerRunnerProvider
    {
        public CompilerRunnerBase GetCompiler(IContentType contentType) { return new MarkdownCompilerRunner(contentType); }
    }
    ///<summary>Compiles files synchronously using MarkdownSharp and reports the results.</summary>
    class MarkdownCompilerRunner : CompilerRunnerBase
    {
        public MarkdownCompilerRunner(IContentType contentType) : base(contentType) { }
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
