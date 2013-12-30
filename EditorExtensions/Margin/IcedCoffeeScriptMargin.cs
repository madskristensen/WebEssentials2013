using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class IcedCoffeeScriptMargin : MarginBase
    {
        public const string MarginName = "IcedCoffeeScriptMargin";

        public IcedCoffeeScriptMargin(string contentType, string source, bool showMargin, ITextDocument document)
            : base(source, MarginName, contentType, showMargin, document)
        { }

        protected override async void StartCompiler(string source)
        {
            if (!CompileEnabled)
                return;

            string icdeCoffeeFilePath = Document.FilePath;

            string jsFileName = GetCompiledFileName(icdeCoffeeFilePath, ".js", CompileToLocation);

            if (IsFirstRun && File.Exists(jsFileName))
            {
                OnCompilationDone(File.ReadAllText(jsFileName), icdeCoffeeFilePath);
                return;
            }

            Logger.Log("IcedCoffeeScript: Compiling " + Path.GetFileName(icdeCoffeeFilePath));

            var result = await new IcedCoffeeScriptCompiler().Compile(icdeCoffeeFilePath, jsFileName);

            if (result.IsSuccess)
            {
                OnCompilationDone(result.Result, result.FileName);
            }
            else
            {
                result.Error.Message = "IcedCoffeeScript: " + result.Error.Message;

                CreateTask(result.Error);

                base.OnCompilationDone("ERROR:" + result.Error.Message, icdeCoffeeFilePath);
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