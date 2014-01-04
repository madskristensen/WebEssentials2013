using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions;

namespace WebEssentialsTests
{
    static class Extensions
    {
        public static async Task<string> CompileString(this NodeExecutorBase compiler, string source, string sourceExtension, string targetExtension)
        {
            var sourceFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + sourceExtension);
            var targetFileName = Path.ChangeExtension(sourceFileName, targetExtension);

            try
            {
                File.WriteAllText(sourceFileName, source);

                var result = await compiler.Compile(sourceFileName, targetFileName);

                if (result.IsSuccess)
                    return result.Result;
                else
                    throw new ExternalException(result.Error.First().Message);
            }
            finally
            {
                File.Delete(sourceFileName);
                File.Delete(targetFileName);
            }
        }
    }
}
