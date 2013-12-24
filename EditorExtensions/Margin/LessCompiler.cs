using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Helpers;
using Microsoft.CSS.Core;

namespace MadsKristensen.EditorExtensions
{
    public class LessCompiler : NodeExecutorBase
    {
        private static readonly Regex _endingCurlyBraces = new Regex(@"}\W*}|}", RegexOptions.Compiled);
        private static readonly Regex _linesStartingWithTwoSpaces = new Regex("(\n( *))", RegexOptions.Compiled);
        private static readonly Regex errorParser = new Regex(@"^(.+) in (.+) on line (\d+), column (\d+):$", RegexOptions.Multiline);

        protected override string Compiler
        {
            get { return @"node_modules\less\bin\lessc"; }
        }

        public override async Task<CompilerResult> RunCompile(string fileName, string targetFileName = null)
        {
            string output = Path.GetTempFileName();
            string arguments = String.Format("--no-color --relative-urls \"{0}\" \"{1}\"", fileName, output);

            if (WESettings.GetBoolean(WESettings.Keys.LessSourceMaps))
            {
                string baseFolder = ProjectHelpers.GetRootFolder() ?? Path.GetDirectoryName(targetFileName);

                arguments = String.Format("--no-color --relative-urls --source-map-basepath=\"{0}\" --source-map=\"{1}.map\" \"{2}\" \"{3}\"",
                    baseFolder.Replace("\\", "/"), targetFileName, fileName, output);
            }

            return await base.Compile(fileName, targetFileName, arguments, output);
        }

        protected override CompilerResult ProcessResult(Process process, string errorText, string fileName, string targetFileName, string output)
        {
            CompilerResult result = new CompilerResult(fileName);

            ProcessResult(output, process, errorText, result);

            if (result.IsSuccess)
            {
                // Inserts an empty row between each rule and replace two space indentation with 4 space indentation
                result.Result = _endingCurlyBraces.Replace(_linesStartingWithTwoSpaces.Replace(result.Result.Trim(), "$1$2"), "$&\n");

                var message = "LESS: " + Path.GetFileName(fileName) + " compiled.";

                // If the caller wants us to renormalize URLs to a different filename, do so.
                if (targetFileName != null && result.Result.IndexOf("url(", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    try
                    {
                        result.Result = CssUrlNormalizer.NormalizeUrls(
                            tree: new CssParser().Parse(result.Result, true),
                            targetFile: targetFileName,
                            oldBasePath: fileName
                        );
                    }
                    catch (Exception ex)
                    {
                        message = "LESS: An error occurred while normalizing generated paths in " + fileName + "\r\n" + ex;
                    }
                }

                Logger.Log(message);
            }
            else
            {
                Logger.Log("LESS: " + Path.GetFileName(fileName) + " compilation failed.");
            }

            return result;
        }

        private static void ProcessResult(string outputFile, Process process, string errorText, CompilerResult result)
        {
            if (!File.Exists(outputFile))
            {
                throw new FileNotFoundException("LESS compiled output not found", outputFile);
            }

            if (process.ExitCode == 0)
            {
                result.Result = File.ReadAllText(outputFile);
                result.IsSuccess = true;
            }
            else
            {
                result.Error = ParseError(errorText.Replace("\r", ""));
            }

            File.Delete(outputFile);
        }

        private static CompilerError ParseError(string error)
        {
            var match = errorParser.Match(error);

            if (!match.Success)
            {
                Logger.Log("LESS parse error: " + error);
                return new CompilerError { Message = error };
            }
            return new CompilerError
            {
                Message = match.Groups[1].Value,
                FileName = match.Groups[2].Value,
                Line = int.Parse(match.Groups[3].Value, CultureInfo.CurrentCulture),
                Column = int.Parse(match.Groups[4].Value, CultureInfo.CurrentCulture)
            };
        }
    }
}