using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions.RazorZen.Converter
{
    public class RazorZenConverter
    {
        public string Convert(string source)
        {
            var target = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < target.Length; i++)
            {
                var line = target[i];

                // Comments
                line = line.Replace("<!--", "!-")
                    .Replace("-->", "-!");

                // Add Space before child
                line = Regex.Replace(line,
                     "(<[^\\/]+>)(<[^\\/]+>)",
                     "$1 $2",
                     RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                // Textnodes
                line = Regex.Replace(line,
                     "(<[^\\/][^<>]+>)(([^<>])+)<\\/",
                     "$1{$2}</",
                     RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                // Ids
                {
                    var classesRegex = new Regex("<[^\\/>]+( id=\")([^>\"]+)(\")[^>]*>",
                        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                    // Match the regular expression pattern against a text string.
                    var m = classesRegex.Match(line);

                    while (m.Success)
                    {
                        // Remove suffix "
                        var g = m.Groups[3];
                        line = line.Remove(g.Index, g.Length);

                        // Replace id1 with #id1
                        g = m.Groups[2];
                        line = line.Remove(g.Index, g.Length)
                             .Insert(g.Index, "#" + g.Value.Trim().Replace(' ', '#'));

                        // Remove Prefix id="
                        g = m.Groups[1];
                        line = line.Remove(g.Index, g.Length);

                        m = m.NextMatch();
                    }
                }

                // Classes
                {
                    var classesRegex = new Regex("<[^\\/>]+( class=\")([^>\"]+)(\")[^>]*>",
                        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                    // Match the regular expression pattern against a text string.
                    var m = classesRegex.Match(line);

                    while (m.Success)
                    {
                        // Remove suffix "
                        var g = m.Groups[3];
                        line = line.Remove(g.Index, g.Length);

                        // Replace class1 class2 to .class1.class2
                        g = m.Groups[2];
                        line = line.Remove(g.Index, g.Length)
                             .Insert(g.Index, "." + g.Value.Trim().Replace(' ', '.'));

                        // Remove Prefix class="
                        g = m.Groups[1];
                        line = line.Remove(g.Index, g.Length);

                        m = m.NextMatch();
                    }
                }

                // Attribtues
                line = Regex.Replace(line,
                     "(<[^\\/<> ]+) ([^<>]+>)",
                     "$1[$2]",
                     RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                // Divs
                line = Regex.Replace(line,
                     "<div([^<>]*)>",
                     "$1",
                     RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                // Tags
                line = Regex.Replace(line,
                     "<([^\\/]([^<>])*)>",
                     "$1",
                     RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                // Close Tags
                line = Regex.Replace(line,
                     "<\\/([^<>])*>",
                     "",
                     RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                target[i] = line;
            }

            return string.Join(Environment.NewLine, target);
        }
    }
}