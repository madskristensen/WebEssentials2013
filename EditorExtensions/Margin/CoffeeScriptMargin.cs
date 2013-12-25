using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    class CoffeeScriptMargin : MarginBase
    {
        public const string MarginName = "CoffeeScriptMargin";

        public CoffeeScriptMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        { }

        protected override async void StartCompiler(string source)
        {
            if (!CompileEnabled)
                return;

            string coffeeFilePath = Document.FilePath;

            string fileName = GetCompiledFileName(coffeeFilePath, ".js", CompileToLocation);//Document.FilePath.Replace(".coffee", ".js");

            if (IsFirstRun && File.Exists(fileName))
            {
                OnCompilationDone(File.ReadAllText(fileName), coffeeFilePath);
                return;
            }

            Logger.Log("CoffeeScript: Compiling " + Path.GetFileName(coffeeFilePath));

            var result = await new CoffeeScriptCompiler().RunCompile(coffeeFilePath, fileName);

            if (result.IsSuccess)
            {
                OnCompilationDone(result.Result, result.FileName);
            }
            else
            {
                result.Error.Message = "CoffeeScript: " + result.Error.Message;

                CreateTask(result.Error);

                base.OnCompilationDone("ERROR:" + result.Error.Message, coffeeFilePath);
            }
        }

        public override void MinifyFile(string fileName, string source)
        {
            if (!CompileEnabled)
                return;

            if (WESettings.GetBoolean(WESettings.Keys.CoffeeScriptMinify))
            {
                FileHelpers.MinifyFile(fileName, source, ".js");
            }
        }

        public override bool CompileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.CoffeeScriptEnableCompiler); }
        }

        public override string CompileToLocation
        {
            get { return WESettings.GetString(WESettings.Keys.CoffeeScriptCompileToLocation); }
        }

        public override bool IsSaveFileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.GenerateJsFileFromCoffeeScript); }
        }

        protected override bool CanWriteToDisk(string source)
        {
            return !string.IsNullOrWhiteSpace(source);
        }
    }
}

//static class Iced
//{
//    [Export]
//    [FileExtension(".iced")]
//    [ContentType("CoffeeScript")]
//    internal static FileExtensionToContentTypeDefinition IcedFileExtensionDefinition;
//}