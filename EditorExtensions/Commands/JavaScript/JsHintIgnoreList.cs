using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
            string parentPath = Path.GetFullPath(Path.GetDirectoryName(path));
            return new JsHintIgnoreList(
                File.ReadLines(path)
                    .Where(p => !String.IsNullOrWhiteSpace(p))
                    .Select(p => new Minimatcher(
                        PrefixedResolve(parentPath, p.Trim()),
                        new Minimatch.Options { AllowWindowsPaths = true, IgnoreCase = true })
                )
            );
        }

        static string PrefixedResolve(string parentPath, string str)
        {
            if (str[0] == '!')
            {
                parentPath = "!" + parentPath;
                str = str.Substring(1);
            }
            // Return absolute paths as-is
            if (str[0] == '/' || str[0] == '\\' || (str.Length > 1 && str[1] == ':'))
                return str;
            // Concatenate the base path to other strings
            return parentPath + "/" + str;
        }
    }
}
