using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Microsoft.Web.Editor.Intellisense;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace MadsKristensen.EditorExtensions
{
    public class NodeModuleCompletionSource : FunctionCompletionSource
    {
        // This won't conflict with RequireJS, since its require() function takes an array rather than a string.
        protected override string FunctionName { get { return "require"; } }

        static ImageSource moduleIcon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/node_module.png", UriKind.RelativeOrAbsolute));

        public override IEnumerable<Completion> GetEntries(char quoteChar, SnapshotPoint caret)
        {
            var callingFilename = caret.Snapshot.TextBuffer.GetFileName();
            var baseFolder = Path.GetDirectoryName(callingFilename);

            //TODO: Find / and show filesystem entries

            return GetAvailableModules(baseFolder)
                    .Select(p => new Completion(
                        quoteChar + Path.GetFileName(p) + quoteChar,
                        quoteChar + Path.GetFileName(p) + quoteChar,
                        GetDescription(p),
                        moduleIcon,
                        "Node module"
                    ));
        }

        ///<summary>Returns all Node.js modules visible from a given directory, including those from node_modules in parent directories.</summary>
        ///<remarks>The modules will be sorted by depth (innermost modules first), then alphabetically.</remarks>
        public static IEnumerable<string> GetAvailableModules(string directory)
        {
            var nmDir = Path.Combine(directory, "node_modules");
            IEnumerable<string> ourModules;
            if (Directory.Exists(nmDir))
                ourModules = Directory.EnumerateDirectories(nmDir)
                    .Where(s => !Path.GetFileName(s).StartsWith("."))
                    .OrderBy(s => s);
            else
                ourModules = Enumerable.Empty<string>();

            var parentDir = Path.GetDirectoryName(directory);
            if (String.IsNullOrEmpty(parentDir))
                return ourModules;
            else
                return ourModules.Concat(GetAvailableModules(parentDir));
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
    }
}