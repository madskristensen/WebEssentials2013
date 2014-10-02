using System;
using System.IO;
using System.Reflection;
using Jurassic;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Handlebars.Compilation
{
    public class Compiler
    {
        public enum Version
        {
            One,
            Two
        }
        public static string GetCompiledTemplateJS(string sourcePath, string targetPath, string template, IContentType contentType)
        {

            var settings = WESettings.Instance.ForContentType<IHandlebarsSettings>(contentType);
            var compiledTemplateName = Path.GetFileNameWithoutExtension(sourcePath); 
            var engine = new ScriptEngine();
            var jsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("MadsKristensen.EditorExtensions.Handlebars.Script.handlebars-v{0}.js", GetVersionNumber(settings)));
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

        public static string GetVersionNumber(IHandlebarsSettings settings)
        {
            switch (settings.CompilerVersion)
            {
                case Version.One:
                    return "1.3.0";
                case Version.Two:
                    return "2.0.0";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
    }
}
