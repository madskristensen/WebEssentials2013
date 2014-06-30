using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.Autoprefixer
{
    public static class CssAutoprefixer
    {
        public static async Task<string> AutoprefixFile(string sourceFileName, string targetFileName, string mapFileName)
        {
            string result = null;
            string tempDirectory = null;
            if (Path.GetDirectoryName(sourceFileName) != Path.GetDirectoryName(targetFileName) || Path.GetDirectoryName(targetFileName) != Path.GetDirectoryName(mapFileName))
            {
                // Create temporary directory - since autoprefixer source maps must be in same directory as output https://www.npmjs.org/package/grunt-autoprefixer#options-map
                tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);

                // Create temporary files
                File.Copy(sourceFileName, Path.Combine(tempDirectory, Path.GetFileName(sourceFileName)));
                File.Copy(targetFileName, Path.Combine(tempDirectory, Path.GetFileName(targetFileName)));
                File.Copy(mapFileName, Path.Combine(tempDirectory, Path.GetFileName(mapFileName)));
            }

            var autoprefixResult = await new AutoprefixerCompiler().CompileAsync(targetFileName, targetFileName);
            if (autoprefixResult.IsSuccess)
            {
                result = autoprefixResult.Result;

                if (tempDirectory != null)
                {
                    // Get the autoprefixer updated source map
                    var updatedSourceMap = await FileHelpers.ReadAllTextRetry(Path.Combine(tempDirectory, Path.GetFileName(mapFileName)));

                    // Write updates to the actual source map file
                    await FileHelpers.WriteAllTextRetry(mapFileName, updatedSourceMap);
                }
            }

            if (tempDirectory != null)
                Directory.Delete(tempDirectory, true);

            return result;
        }
    }
}
