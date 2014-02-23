using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Compilers
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType("SASS")]
    public class SassCompiler : CssCompilerBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\node-sass\bin\node-sass");
        private static readonly Regex _errorParsingPattern = new Regex(@"(?<fileName>.*):(?<line>.\d*): error: (?<message>.*\n.*)", RegexOptions.Multiline);

        public override string ServiceName { get { return "SASS"; } }
        public override string TargetExtension { get { return ".css"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        protected override Regex ErrorParsingPattern { get { return _errorParsingPattern; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.Sass.GenerateSourceMaps; } }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            var args = new StringBuilder();

            if (GenerateSourceMap)
            {
                args.Append("--source-map ");
            }

            args.AppendFormat(CultureInfo.CurrentCulture, "--output-style=expanded \"{0}\" --output \"{1}\"", sourceFileName, targetFileName);

            return args.ToString();
        }

        //https://github.com/hcatlin/libsass/issues/242
        protected override string ReadMapFile(string sourceMapFileName)
        {
            return File.ReadAllText(sourceMapFileName).Replace("\\", "\\\\");
        }
    }
}