using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Intel = Microsoft.VisualStudio.Language.Intellisense;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    [Export(typeof(ICompletionSourceProvider))]
    [Order(Before = "High")]
    [ContentType("JavaScript"),
    Name("GruntTaskCompletion")]
    public class GruntTaskCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new GruntTaskCompletionSource(textBuffer)) as ICompletionSource;
        }
    }

    public class GruntTaskCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;

        public GruntTaskCompletionSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            var position = session.GetTriggerPoint(_buffer).GetPoint(_buffer.CurrentSnapshot);
            var line = position.GetContainingLine();

            if (line == null) return;

            int linePos = position - line.Start.Position;

            var info = GruntTaskCompletionUtils.FindCompletionInfo(line.GetText(), linePos);
            if (info == null) return;

            var callingFilename = _buffer.GetFileName();
            var baseFolder = Path.GetDirectoryName(callingFilename);

            IEnumerable<Intel.Completion> results = new List<Intel.Completion>();
            if (String.IsNullOrWhiteSpace(info.Item1))
                results = GetRootCompletions(baseFolder);

            var trackingSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(info.Item2.Start + line.Start, info.Item2.Length, SpanTrackingMode.EdgeInclusive);
            completionSets.Add(new CompletionSet(
                "Node.js Modules",
                "Node.js Modules",
                trackingSpan,
                results,
                null
            ));

        }

        #region Root-level completions

        static ImageSource moduleIcon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/node_module.png", UriKind.RelativeOrAbsolute));

        static IEnumerable<Intel.Completion> GetRootCompletions(string baseFolder)
        {
            return NodeModuleService.GetAvailableModules(baseFolder)
                    .Select(p => new Intel.Completion(
                        Path.GetFileName(p),
                        Path.GetFileName(p),
                        GetDescription(p).ConfigureAwait(false).GetAwaiter().GetResult(),
                        moduleIcon,
                        "Node module"
                    )).Where(t => t.DisplayText != "grunt");
        }

        static async Task<string> GetDescription(string path)
        {
            var packageFile = Path.Combine(path, "package.json");
            if (!File.Exists(packageFile))
                return "This module does not have a package.json file.";
            try
            {
                var json = JObject.Parse(await FileHelpers.ReadAllTextRetry(packageFile).ConfigureAwait(false));
                return (json.Value<string>("description") ?? "This module's package.json does not have a description property.")
                     + "\nv" + (json.Value<string>("version") ?? "?");
            }
            catch (Exception ex)
            {
                return "An error occurred while reading this module's package.json: " + ex.Message;
            }
        }

        #endregion

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
    ///<summary>Contains host-agnostic methods used to provide Node.js module completions.</summary>
    ///<remarks>This is a separate class so that it can be unit-tested without running any
    ///field initializers that require the VS hosting environment.</remarks>
    public static class GruntTaskCompletionUtils
    {
        static readonly Regex regex = new Regex(@"(?<=\bloadNpmTasks\s*\(\s*(['""]))[a-z0-9_./+=-]*(?=\s*\1\s*\)?)?", RegexOptions.IgnoreCase);
        public static Tuple<string, Span> FindCompletionInfo(string line, int cursorPosition)
        {
            var match = regex.Matches(line)
                             .Cast<Match>()
                             .FirstOrDefault(m => m.Index <= cursorPosition && cursorPosition <= m.Index + m.Length);
            if (match == null)
                return null;

            string prefix = null;

            int precedingSlash;
            if (cursorPosition == match.Index + match.Length)
                precedingSlash = match.Value.LastIndexOf('/');
            else if (cursorPosition == match.Index)
                precedingSlash = -1;
            else
                precedingSlash = match.Value.LastIndexOf('/', cursorPosition - match.Index - 1);

            if (precedingSlash >= 0)
            {
                precedingSlash++;       // Skip the slash character
                prefix = match.Value.Substring(0, precedingSlash);  // Remove(precedingSlash) fails if the / is at the end
            }
            else
                precedingSlash = 0;


            var followingSlash = match.Value.IndexOf('/', cursorPosition - match.Index);
            if (followingSlash < 0)
                followingSlash = match.Length;

            return Tuple.Create(
                prefix,
                Span.FromBounds(precedingSlash + match.Index, followingSlash + match.Index)
            );
        }
    }
}
