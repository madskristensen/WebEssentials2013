using MadsKristensen.EditorExtensions.Settings;
using Microsoft.Html.Editor;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.Misc.Autoprefixer
{
    public static class Autoprefixer 
    {
        public static async Task<string> AutoprefixContent(string resultSource, string targetFileName, bool generateSourceMap)
        {
            if (!WESettings.Instance.Css.Autoprefix)
                return resultSource;

            var result = await new AutoprefixerCompiler(generateSourceMap).CompileAsync(targetFileName, targetFileName);
            return result.Result;
        }
    }
}
