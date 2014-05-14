using MadsKristensen.EditorExtensions.Settings;
using Microsoft.Html.Editor;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MadsKristensen.EditorExtensions.Misc.Autoprefixer
{
    public static class Autoprefixer 
    {
        public static async Task<string> AutoprefixContent(string content)
        {
            var settings = WESettings.Instance.ForContentType<IAutoprefixerSettings>(ContentTypeManager.GetContentType("CSS"));
            if (settings == null || !settings.Autoprefix)
                return content;

            var tempName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".autoprefix");

            await FileHelpers.WriteAllTextRetry(tempName, content);

            if (!File.Exists(tempName))
                return content;

            var result = await new AutoprefixerCompiler().CompileAsync(tempName, tempName);

            File.Delete(tempName);

            return result.Result;
        }
    }
}
