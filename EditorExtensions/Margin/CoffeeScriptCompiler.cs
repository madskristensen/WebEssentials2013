using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace MadsKristensen.EditorExtensions
{
    [ComVisible(true)]  // Required to expose this instance to WebBrowser for JS
    public class CoffeeScriptCompiler : ScriptRunnerBase
    {
        public CoffeeScriptCompiler() { }

        public CoffeeScriptCompiler(Dispatcher dispatcher)
            : base(dispatcher)
        { }

        protected override string CreateHtml(string source, string fileName)
        {
            string clean = source
                .Replace("\\", "\\\\")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("'", "\\'");

            string bare = WESettings.GetBoolean(WESettings.Keys.WrapCoffeeScriptClosure) ? "false" : "true";
            string script = ReadResourceFile("MadsKristensen.EditorExtensions.Resources.Scripts.IcedCoffeeScript-1.6.3-f.js");

            script +=
                    "try{" +
                        "var result = CoffeeScript.compile('" + clean + "', { bare: " + bare + ", runtime:'inline' });" +
                        "window.external.Execute(result, '" + fileName.Replace("\\", "\\\\") + "');" +
                    "}" +
                    "catch (err){" +
                        "var locationMsg = '';" +
                        "if (err && err.location) {" +
                            "locationMsg = err.location.first_line + ':' + err.location.first_column + ':';" +
                        "}" +
                        "window.external.Execute('ERROR:'+locationMsg+err, '" + fileName.Replace("\\", "\\\\") + "');" +
                    "}";

            return "<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=9\" /><script>" + script + "</script></head><html/>";
        }
    }
}