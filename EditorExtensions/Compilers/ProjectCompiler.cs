using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
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
            var exts = new HashSet<string>(
                FileExtensionRegistry.GetExtensionsForContentType(contentType)
                                        .Select(e => "." + e),
                StringComparer.OrdinalIgnoreCase
            );
            var runner = Mef.GetImport<ICompilerRunnerProvider>(contentType).GetCompiler(contentType);

            return Task.WhenAll(
                paths.Where(f => exts.Contains(Path.GetExtension(f)))
                    .AsParallel()
                    .Select(fileName =>
                        {
                            string targetPath = runner.GetTargetPath(fileName);
                            if (File.Exists(targetPath))
                                return runner.CompileAsync(fileName, targetPath);
                            else
                                return Task.FromResult<CompilerResult>(null);
                        })
            );
        }
    }
}
