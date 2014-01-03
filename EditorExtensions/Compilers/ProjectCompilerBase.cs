using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;

namespace MadsKristensen.EditorExtensions
{
    internal abstract class ProjectCompilerBase
    {
        protected abstract string ServiceName { get; }
        protected abstract string CompileToExtension { get; }
        protected abstract string CompileToLocation { get; }
        protected abstract NodeExecutorBase Compiler { get; }
        protected abstract IEnumerable<string> Extensions { get; }

        public async Task CompileProject(Project project)
        {
            if (project != null && !string.IsNullOrEmpty(project.FullName))
            {
                var folder = ProjectHelpers.GetRootFolder(project);
                var sourceFiles = Extensions.SelectMany(e => Directory.EnumerateFiles(folder, "*." + e, SearchOption.AllDirectories));

                await Compile(sourceFiles);
            }
        }

        protected Task Compile(IEnumerable<string> sourceFiles)
        {
            return Task.WhenAll(sourceFiles.Select(async sourceFile =>
            {
                string compiledFile = MarginBase.GetCompiledFileName(sourceFile, CompileToExtension, CompileToLocation);
                var result = await Compiler.Compile(sourceFile, compiledFile);

                if (result.IsSuccess)
                    FileHelpers.WriteResult(result, compiledFile, CompileToExtension);
                else
                    Logger.Log(result.Error.Message ?? (String.Format(CultureInfo.CurrentCulture, "Error compiling {0} file: {1}", ServiceName, sourceFile)));
            }));
        }
    }
}
