using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Compilers
{
    [Export]
    public class ProjectCompiler
    {
        [Import]
        public IFileExtensionRegistryService FileExtensionRegistry { get; set; }

        public Task CompileSolutionAsync(IContentType contentType)
        {
            return CompileFilesAsync(contentType,
                ProjectHelpers.GetAllProjects()
                              .Select(ProjectHelpers.GetRootFolder)
                              .Where(p => !string.IsNullOrEmpty(p))
                              .SelectMany(p => Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
            );
        }
        public Task CompileFilesAsync(IContentType contentType, IEnumerable<string> paths)
        {
            var exts = FileExtensionRegistry.GetFileExtensionSet(contentType);
            var runner = Mef.GetImport<ICompilerRunnerProvider>(contentType).GetCompiler(contentType);
            var filesToCheck = paths.Where(f => exts.Contains(Path.GetExtension(f)))
                .ToDictionary(fileName => fileName, filePath => runner.GetTargetPath(filePath))
                .Where(kvp => File.Exists(kvp.Value));

            bool shouldRecompile = filesToCheck.Any(kvp => File.GetLastWriteTime(kvp.Key) > File.GetLastWriteTime(kvp.Value));

            if (shouldRecompile)
            {
                return Task.WhenAll(
                    filesToCheck
                    .AsParallel()
                    .Select(kvp =>
                    {
                        return runner.CompileAsync(kvp.Key, kvp.Value).HandleErrors("compiling" + kvp.Key);
                    })
                );
            }
            return Task.FromResult<CompilerResult>(null);
        }
    }
}
