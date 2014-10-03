using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Commands;
using MadsKristensen.EditorExtensions.Compilers;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Handlebars.Compilation
{
    [Export(typeof(IFileSaveListener))]
    [ContentType("Handlebars")]
    class CompilationSaveListener : IFileSaveListener
    {
        public async Task FileSaved(IContentType contentType, string path, bool forceSave, bool minifyInPlace)
        {
            var settings = WESettings.Instance.ForContentType<ICompilerInvocationSettings>(contentType);
            if (settings == null || !settings.CompileOnSave)
                return;

            await CompileFile(contentType, path);


        }

        public async static Task CompileFile(IContentType contentType, string sourcePath)
        {
            if (!ShoudlCompile(sourcePath))
                return;
            var compiler = Mef.GetImport<ICompilerRunnerProvider>(contentType).GetCompiler(contentType);
            await compiler.CompileToDefaultOutputAsync(sourcePath);
        }

        public static bool ShoudlCompile(string path)
        {
            var baseName = Path.GetFileNameWithoutExtension(path);
            return !baseName.EndsWith(".js", StringComparison.OrdinalIgnoreCase);
        }

    }
}
