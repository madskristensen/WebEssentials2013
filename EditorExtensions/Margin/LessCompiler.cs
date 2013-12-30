using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    public class LessCompiler : NodeExecutorBase
    {
        private static readonly Regex _endingCurlyBraces = new Regex(@"}\W*}|}", RegexOptions.Compiled);
        private static readonly Regex _linesStartingWithTwoSpaces = new Regex("(\n( *))", RegexOptions.Compiled);
        private static readonly Regex _errorParsingPattern = new Regex(@"^(?<message>.+) in (?<fileName>.+) on line (?<line>\d+), column (?<column>\d+):$", RegexOptions.Multiline);

        protected override string ServiceName
        {
            get { return "LESS"; }
        }
        protected override string CompilerPath
        {
            get { return @"node_modules\less\bin\lessc"; }
        }
        protected override Regex ErrorParsingPattern
        {
            get { return _errorParsingPattern; }
        }
        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            var args = new StringBuilder("--no-color --relative-urls ");

            if (WESettings.GetBoolean(WESettings.Keys.LessSourceMaps))
            {
                string baseFolder = ProjectHelpers.GetProjectFolder(targetFileName) ?? Path.GetDirectoryName(targetFileName);

                args.AppendFormat(CultureInfo.CurrentCulture, "--source-map-basepath=\"{0}\" --source-map=\"{1}.map\"",
                    baseFolder.Replace("\\", "/"), targetFileName);
            }

            args.AppendFormat(CultureInfo.CurrentCulture, "\"{0}\" \"{1}\"", sourceFileName, targetFileName);
            return args.ToString();
        }

        protected override string PostProcessResult(string resultSource, string sourceFileName, string targetFileName)
        {
            // Inserts an empty row between each rule and replace two space indentation with 4 space indentation
            resultSource = _endingCurlyBraces.Replace(_linesStartingWithTwoSpaces.Replace(resultSource.Trim(), "$1$2"), "$&\n");

            var message = "LESS: " + Path.GetFileName(sourceFileName) + " compiled.";

            // If the caller wants us to renormalize URLs to a different filename, do so.
            if (targetFileName != null && resultSource.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
            {
                try
                {
                    resultSource = CssUrlNormalizer.NormalizeUrls(
                        tree: new CssParser().Parse(resultSource, true),
                        targetFile: targetFileName,
                        oldBasePath: sourceFileName
                    );
                }
                catch (Exception ex)
                {
                    message = "LESS: An error occurred while normalizing generated paths in " + sourceFileName + "\r\n" + ex;
                }
            }

            Logger.Log(message);

            return resultSource;
        }
    }
}