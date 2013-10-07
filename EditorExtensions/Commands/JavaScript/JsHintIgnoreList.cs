using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minimatch;

namespace MadsKristensen.EditorExtensions.Commands.JavaScript
{
    ///<summary>Contains a parsed representation of a .jshintignore file.</summary>
    class JsHintIgnoreList
    {
        readonly ReadOnlyCollection<Minimatcher> matchers;

        public JsHintIgnoreList(IEnumerable<Minimatcher> matchers)
        {
            this.matchers = matchers.ToList().AsReadOnly();
        }

        // Based on https://github.com/jshint/jshint/blob/83374ad/src/cli.js#L168-L219

        public bool IsIgnored(string path)
        {
            path = Path.GetFullPath(path);
            return matchers.Any(m => m.IsMatch(path));
        }

        public static JsHintIgnoreList Load(string path)
        {
            string parentPath = Path.GetDirectoryName(path);
            return new JsHintIgnoreList(
                File.ReadLines(path)
                    .Where(p => !String.IsNullOrWhiteSpace(p))
                    .Select(p => new Minimatcher(
                        PrefixedResolve(parentPath, path.Trim()),
                        new Minimatch.Options { AllowWindowsPaths = true, IgnoreCase = true })
                )
            );
        }

        static string PrefixedResolve(string parentPath, string str)
        {
            if (str[0] == '!')
                return "!" + Path.GetFullPath(Path.Combine(parentPath, str.Substring(1)));
            return Path.GetFullPath(Path.Combine(parentPath, str));
        }
    }
}
