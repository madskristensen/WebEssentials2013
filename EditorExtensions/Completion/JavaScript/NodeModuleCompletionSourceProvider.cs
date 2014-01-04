using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Newtonsoft.Json.Linq;
using Intel = Microsoft.VisualStudio.Language.Intellisense;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICompletionSourceProvider))]
    [Order(Before = "High")]
    [ContentType("JavaScript"),
    Name("NodeJsCompletion")]
    public class NodeModuleCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new NodeModuleCompletionSource(textBuffer)) as ICompletionSource;
        }
    }

    public class NodeModuleCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;

        public NodeModuleCompletionSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            var position = session.GetTriggerPoint(_buffer).GetPoint(_buffer.CurrentSnapshot);
            var line = position.GetContainingLine();

            if (line == null) return;

            int linePos = position - line.Start.Position;

            var info = NodeModuleCompletionUtils.FindCompletionInfo(line.GetText(), linePos);
            if (info == null) return;

            var callingFilename = _buffer.GetFileName();
            var baseFolder = Path.GetDirectoryName(callingFilename);

            IEnumerable<Intel.Completion> results;
            if (String.IsNullOrWhiteSpace(info.Item1))
                results = GetRootCompletions(baseFolder);
            else
            {
                results = GetRelativeCompletions(NodeModuleService.ResolvePath(baseFolder, info.Item1));
                // Show completions for ../../
                if (parentTraversalRegex.IsMatch(info.Item1))
                    results = new[] { parentFolder }.Concat(results);
            }

            var trackingSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(info.Item2.Start + line.Start, info.Item2.Length, SpanTrackingMode.EdgeInclusive);
            completionSets.Add(new CompletionSet(
                "Node.js Modules",
                "Node.js Modules",
                trackingSpan,
                results,
                null
            ));

        }

        static readonly Regex parentTraversalRegex = new Regex(@"^(\.\./)+$");
        static readonly ImageSource folderIcon = GlyphService.GetGlyph(StandardGlyphGroup.GlyphOpenFolder, StandardGlyphItem.GlyphItemPublic);
        #region Root-level completions
        static ImageSource moduleIcon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/node_module.png", UriKind.RelativeOrAbsolute));
        static readonly Intel.Completion parentFolder = new Intel.Completion("../", "../", "Prefix to access files in the parent directory", folderIcon, "Folder");
        static readonly ReadOnlyCollection<Intel.Completion> dotCompletions = new ReadOnlyCollection<Intel.Completion>(new[]{
            new Intel.Completion("./", "./", "Prefix to access files in the current directory", folderIcon, "Folder"),
            parentFolder
        });

        // Gathered from `require('<tab><tab>` on Node v0.10.15.
        static readonly ReadOnlyCollection<Intel.Completion> builtInModuleCompletions = new ReadOnlyCollection<Intel.Completion>(Array.ConvertAll(new[]{
                "assert", "buffer", "child_process", "cluster", "crypto", "dgram", "dns", "domain", "events", 
                "fs", "http", "https", "net", "os",  "path", "punycode", "querystring", "readline", "stream", 
                "string_decoder", "tls", "tty", "url", "util", "vm", "zlib"
        }, m => new Intel.Completion(m, m, null, moduleIcon, "Node module")));
        static IEnumerable<Intel.Completion> GetRootCompletions(string baseFolder)
        {
            return dotCompletions
                    .Concat(NodeModuleService.GetAvailableModules(baseFolder)
                        .Select(p => new Intel.Completion(
                            Path.GetFileName(p),
                            Path.GetFileName(p),
                            GetDescription(p),
                            moduleIcon,
                            "Node module"
                        ))
                    ).Concat(builtInModuleCompletions);
        }
        static string GetDescription(string path)
        {
            var packageFile = Path.Combine(path, "package.json");
            if (!File.Exists(packageFile))
                return "This module does not have a package.json file.";
            try
            {
                var json = JObject.Parse(File.ReadAllText(packageFile));
                return (json.Value<string>("description") ?? "This module's package.json does not have a description property.")
                     + "\nv" + (json.Value<string>("version") ?? "?");
            }
            catch (Exception ex)
            {
                return "An error occurred while reading this module's package.json: " + ex.Message;
            }
        }
        #endregion

        #region Directory-level completions
        static readonly IReadOnlyDictionary<string, ImageSource> fileIcons = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase)
        {
            { ".js",    BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/jsfile.png", UriKind.RelativeOrAbsolute)) },
            { ".json",  GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupJSharpNamespace, StandardGlyphItem.GlyphItemPublic) },
            { ".node",  GlyphService.GetGlyph(StandardGlyphGroup.GlyphLibrary, StandardGlyphItem.GlyphItemPublic) }
        };

        static IEnumerable<Intel.Completion> GetRelativeCompletions(string folder)
        {
            if (folder == null || !Directory.Exists(folder))
                return null;

            return Directory.EnumerateDirectories(folder)
                            .OrderBy(s => s)
                            .Select(p => new Intel.Completion(
                                Path.GetFileName(p) + "/",
                                Path.GetFileName(p) + "/",
                                null,
                                folderIcon,
                                "Directory"
                            ))
                .Concat(
                    Directory.EnumerateFiles(folder)
                             .Where(p => fileIcons.ContainsKey(Path.GetExtension(p).ToLowerInvariant()))
                             .OrderBy(s => s)
                             .Select(p => new Intel.Completion(
                                 Path.GetFileName(p),
                                 Path.GetFileName(p),
                                 null,
                                 fileIcons[Path.GetExtension(p)],
                                 Path.GetExtension(p) + " file"
                             ))

                );
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
    public static class NodeModuleCompletionUtils
    {
        static readonly Regex regex = new Regex(@"(?<=\brequire\s*\(\s*(['""]))[a-z0-9_./+=-]*(?=\s*\1\s*\)?)?", RegexOptions.IgnoreCase);
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
