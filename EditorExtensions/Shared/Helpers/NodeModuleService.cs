using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MadsKristensen.EditorExtensions
{
    ///<summary>Resolves paths to Node.js modules.</summary>
    public static class NodeModuleService
    {
        ///<summary>The default extensions used to resolve modules without extensions.</summary>
        ///<remarks>This must match require.extensions from Node.js; see https://github.com/joyent/node/blob/master/lib/module.js#L464-L484. </remarks>
        private static readonly ReadOnlyCollection<string> _moduleExtensions = new ReadOnlyCollection<string>(new[] { "", ".js", ".json", ".node" });

        public static ReadOnlyCollection<string> ModuleExtensions { get { return _moduleExtensions; } }

        ///<summary>Resolves the full path to the JS file that will be loaded by a call to require().  This will always return an existing file (not directory) on disk, or null.</summary>
        ///<param name="callerPath">The path to the directory containing the file that is calling require().  This must be an absolute path.</param>
        ///<param name="modulePath">The string passed to require().  This can be a relative path, a module name, or a path within a module name.</param>
        ///<returns>The path to the fully resolved file, after resolving default extensions, default directory files, and package.json.</returns>
        public async static Task<string> ResolveModule(string callerDirectory, string modulePath)
        {
            string rawPath = ResolvePath(callerDirectory, modulePath);

            // If we couldn't locate the module at all, or if we got an actual file, stop.
            if (rawPath == null || File.Exists(rawPath))
                return rawPath;

            // Build a list of file-like paths to try with various extensions.
            // In order, try the original path, the 'package.json' main entry,
            // and any index file.
            // https://github.com/joyent/node/blob/master/lib/module.js#L155
            var potentialPaths = new List<string> { rawPath };

            // If the non-existent file is a directory, look inside.
            if (Directory.Exists(rawPath))
            {
                var mainEntry = await GetPackageMain(rawPath).ConfigureAwait(false);
                if (mainEntry != null)
                {
                    potentialPaths.Add(Path.Combine(rawPath, mainEntry));
                    potentialPaths.Add(Path.Combine(rawPath, mainEntry, "index"));
                }
                potentialPaths.Add(Path.Combine(rawPath, "index"));
            }
            // Don't try to resolve a path with a trailing / as a file.
            potentialPaths.RemoveAll(p => p.EndsWith("/", StringComparison.Ordinal));

            // Try adding the default extensions to each potential path we've found, then give up.
            return potentialPaths.SelectMany(path => ModuleExtensions.Select(e => path + e))
                                 .FirstOrDefault(File.Exists);
        }

        ///<summary>Gets the value of the "main" entry in a directory's package.json, or null if it doesn't exist or is invalid.</summary>
        private async static Task<string> GetPackageMain(string directory)
        {
            var packageFile = Path.Combine(directory, "package.json");
            if (!File.Exists(packageFile))
                return null;
            try
            {
                var json = JObject.Parse(await FileHelpers.ReadAllTextRetry(packageFile).ConfigureAwait(false));
                return json.Value<string>("main");
            }
            catch (Exception ex)
            {
                Logger.Log("An error occurred while reading " + packageFile + ": " + ex.Message);
                return null;
            }
        }

        ///<summary>Resolves a relative or module-based path to a directory or file.  This will return the raw value of the path, without resolving default filenames, extensions, or package.json entries for directories.</summary>
        ///<param name="callerPath">The path to the directory containing the file that is calling require().  This must be an absolute path.</param>
        ///<param name="modulePath">The string passed to require().  This can be a relative path, a module name, or a path within a module name.</param>
        ///<returns>The resulting absolute path (which may not exist on disk), or null if no such module could be found.</returns>
        public static string ResolvePath(string callerDirectory, string modulePath)
        {
            var s = modulePath.IndexOf('/');
            string moduleName = s < 0 ? modulePath : modulePath.Remove(s);
            string basePath;
            switch (moduleName)
            {
                case ".":
                    basePath = callerDirectory;
                    break;
                case "..":
                    basePath = Path.GetDirectoryName(callerDirectory);
                    break;
                default:
                    basePath = GetAvailableModules(callerDirectory).FirstOrDefault(m => Path.GetFileName(m) == moduleName);
                    break;
            }

            // If there is no subpath, or if we couldn't find the module, stop.
            if (moduleName == modulePath || basePath == null)
                return basePath;

            var subPath = modulePath.Substring(s + 1);
            return Path.GetFullPath(Path.Combine(basePath, subPath));
            //TODO: Call http://msdn.microsoft.com/en-us/library/aa364962(VS.85).aspx to resolve symlinks, like Node does.
        }

        ///<summary>Returns all Node.js modules visible from a given directory, including those from node_modules in parent directories.</summary>
        ///<remarks>The modules will be sorted by depth (innermost modules first; mirroring require()'s search order), then alphabetically.</remarks>
        public static IEnumerable<string> GetAvailableModules(string directory)
        {
            // This null check is for Node.js projects (NTVS)
            if (string.IsNullOrEmpty(directory))
                return Enumerable.Empty<string>();

            var nmDir = Path.Combine(directory, "node_modules");
            IEnumerable<string> ourModules;
            if (Directory.Exists(nmDir) && Path.GetFileName(directory) != "node_modules")   // don't search in node_modules/node_modules
                ourModules = Directory.EnumerateDirectories(nmDir)
                    .Where(s => !Path.GetFileName(s).StartsWith(".", StringComparison.Ordinal))
                    .Concat(Directory.EnumerateFiles(nmDir, "*.js").Select(p => Path.ChangeExtension(p, null)))
                    .OrderBy(s => s);
            else
                ourModules = Enumerable.Empty<string>();

            var parentDir = Path.GetDirectoryName(directory);
            if (String.IsNullOrEmpty(parentDir))
                return ourModules;
            else
                return ourModules.Concat(GetAvailableModules(parentDir));
        }
    }
}