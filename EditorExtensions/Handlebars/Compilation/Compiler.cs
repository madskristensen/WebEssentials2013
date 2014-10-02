using System.IO;
using System.Reflection;
using Jurassic;

namespace MadsKristensen.EditorExtensions.Handlebars.Compilation
{
    public class Compiler
    {
        public static string GetCompiledTemplateJS(string sourcePath, string targetPath, string template)
        {
            var compiledTemplateName = Path.GetFileNameWithoutExtension(sourcePath); 
            var engine = new ScriptEngine();
            var jsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MadsKristensen.EditorExtensions.Handlebars.Script.handlebars-v2.0.0.js");
            string js;
            using (var reader = new StreamReader(jsStream))
            {
                js = reader.ReadToEnd();
            }
            engine.Execute(js);
            engine.Execute(@"var precompile = Handlebars.precompile;");
            var compiled = string.Format("var {0} = Handlebars.template({1});", compiledTemplateName, engine.CallGlobalFunction("precompile", template).ToString());
            return compiled;
        }
    }
}
