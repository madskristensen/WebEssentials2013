using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Commands;
using MarkdownSharp;
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
        public abstract bool GenerateSourceMap { get; }
        public abstract string TargetExtension { get; }
        public IContentType SourceContentType { get; private set; }
        public IContentType TargetContentType { get; private set; }
        public ICompilerInvocationSettings Settings { get; private set; }

        [Import]
        public IFileExtensionRegistryService FileExtensionRegistry { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "I can't think of a better design.  (this is ugly)")]
        protected CompilerRunnerBase(IContentType contentType)
        {
            Mef.SatisfyImportsOnce(this);
            SourceContentType = contentType;
            TargetContentType = FileExtensionRegistry.GetContentTypeForExtension(TargetExtension.TrimEnd('.'));

            _listeners = Mef.GetAllImports<IFileSaveListener>(TargetContentType);
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
            if (!ShouldCompile(sourcePath))
                return CompileInMemoryAsync(sourcePath);
            var targetPath = Path.GetFullPath(GetTargetPath(sourcePath));
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            return CompileAsync(sourcePath, targetPath);
        }

        private static ISet<string> _disallowedParentExtensions = new HashSet<string> { ".png", ".jpg", ".jpeg", ".gif" };

        ///<summary>Checks whether a file should never be compiled to disk, based on filename conventions.</summary>
        public static bool ShouldCompile(string sourcePath)
        {
            if (Path.GetFileName(sourcePath).StartsWith("_", StringComparison.OrdinalIgnoreCase))
                return false;

            var parentExtension = Path.GetExtension(Path.GetFileNameWithoutExtension(sourcePath));
            return !_disallowedParentExtensions.Contains(parentExtension);
        }

        ///<summary>Gets the default save location for the compiled results of the specified file, based on user settings.</summary>
        public string GetTargetPath(string sourcePath)
        {
            var ext = TargetExtension;

            if (Settings.MinifyInPlace && WESettings.Instance.ForContentType<IMinifierSettings>(TargetContentType).AutoMinify)
                ext = ".min" + ext;

            if (string.IsNullOrEmpty(Settings.OutputDirectory))
                return Path.ChangeExtension(sourcePath, ext);

            string compiledFileName = Path.GetFileName(Path.ChangeExtension(sourcePath, ext));
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
            var result = await RunCompilerAsync(sourcePath, targetPath);

            if (result.IsSuccess && !string.IsNullOrEmpty(targetPath))
            {
                ProjectHelpers.AddFileToProject(sourcePath, targetPath);

                if (GenerateSourceMap && File.Exists(targetPath + ".map"))
                    ProjectHelpers.AddFileToProject(targetPath, targetPath + ".map");

                foreach (var listener in _listeners)
                    listener.FileSaved(TargetContentType, result.TargetFileName, true, Settings.MinifyInPlace);
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
    [ContentType(SweetJsContentTypeDefinition.SweetJsContentType)]
    public class NodeCompilerRunnerProvider : ICompilerRunnerProvider
    {
        public CompilerRunnerBase GetCompiler(IContentType contentType) { return new NodeCompilerRunner(contentType); }
    }

    ///<summary>Compiles files using <see cref="NodeExecutorBase"/> classes and reports the results.</summary>
    class NodeCompilerRunner : CompilerRunnerBase
    {
        public NodeCompilerRunner(IContentType contentType)
            : base(contentType)
        {
            Compiler = Mef.GetImport<NodeExecutorBase>(contentType);
        }

        public NodeExecutorBase Compiler { get; private set; }

        public override string TargetExtension
        {
            // This is called by the base ctor, before we assign Compiler
            get { return (Compiler ?? Mef.GetImport<NodeExecutorBase>(SourceContentType)).TargetExtension; }
        }
        public override bool GenerateSourceMap { get { return Compiler.GenerateSourceMap; } }
        protected override async Task<CompilerResult> RunCompilerAsync(string sourcePath, string targetPath)
        {
            bool isTemp = false;
            if (string.IsNullOrEmpty(targetPath))
            {
                isTemp = true;
                if (!Compiler.RequireMatchingFileName)
                    targetPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Compiler.TargetExtension);
                else
                {
                    // If the compiler cannot compile to a random filename,
                    // generate a unique directory to avoid conflicts.
                    targetPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "-" + Environment.TickCount, Path.GetFileNameWithoutExtension(sourcePath) + Compiler.TargetExtension);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                }
            }

            try
            {
                return await Compiler.CompileAsync(sourcePath, targetPath);
            }
            finally
            {
                if (isTemp)
                {
                    File.Delete(targetPath);
                    File.Delete(targetPath + ".map");   //  Idempotent
                    if (Compiler.RequireMatchingFileName)
                        Directory.Delete(Path.GetDirectoryName(targetPath), true);
                }
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
        public override bool GenerateSourceMap { get { return false; } }
        public override string TargetExtension { get { return ".html"; } }
        protected override Task<CompilerResult> RunCompilerAsync(string sourcePath, string targetPath)
        {
            var result = new Markdown(WESettings.Instance.Markdown).Transform(File.ReadAllText(sourcePath));
            if (!string.IsNullOrEmpty(targetPath))
            {
                ProjectHelpers.CheckOutFileFromSourceControl(targetPath);   // TODO: Only if output changed?
                File.WriteAllText(targetPath, result, new UTF8Encoding(false));
            }

            return Task.FromResult(new CompilerResult(sourcePath, targetPath) { IsSuccess = true, Result = result });
        }
    }
}
