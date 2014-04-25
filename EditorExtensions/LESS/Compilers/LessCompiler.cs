using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Less
{
    [Export(typeof(NodeExecutorBase))]
    [ContentType("LESS")]
    public class LessCompiler : CssCompilerBase
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\less\bin\lessc");
        private static readonly Regex _errorParsingPattern = new Regex(@"^(?<message>.+) in (?<fileName>.+) on line (?<line>\d+), column (?<column>\d+):$", RegexOptions.Multiline);

        public override string TargetExtension { get { return ".css"; } }
        public override string ServiceName { get { return "LESS"; } }
        protected override string CompilerPath { get { return _compilerPath; } }
        protected override Regex ErrorParsingPattern { get { return _errorParsingPattern; } }
        public override bool GenerateSourceMap { get { return WESettings.Instance.Less.GenerateSourceMaps; } }

        protected override string GetArguments(string sourceFileName, string targetFileName, string mapFileName)
        {
            string mapDirectory = Path.GetDirectoryName(mapFileName);

            return string.Format(CultureInfo.CurrentCulture, "--no-color --relative-urls --source-map-basepath=\"{0}\" --source-map=\"{1}\" \"{2}\" \"{3}\"",
                                 mapDirectory, mapFileName, sourceFileName, targetFileName);
        }
    }
}