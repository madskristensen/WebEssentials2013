using System;
using System.IO;
using System.Linq;
using Minimatch;

namespace MadsKristensen.EditorExtensions.Settings
{
    public static class WEIgnore
    {
        public static bool TestWEIgnore(string sourcePath, string serviceToken, string serviceName)
        {
            string ignoreFile = GetIgnoreFile(Path.GetDirectoryName(sourcePath), ".weignore");

            if (string.IsNullOrEmpty(ignoreFile))
                return false;

            string searchPattern;

            using (StreamReader reader = File.OpenText(ignoreFile))
            {
                while ((searchPattern = reader.ReadLine()) != null)
                {
                    searchPattern = searchPattern.Trim();

                    if (string.IsNullOrEmpty(searchPattern) || searchPattern[0] == '!') // Negated pattern
                        continue;

                    int index = searchPattern.LastIndexOf('\t');

                    if (index > 0)
                    {
                        string[] subparts = searchPattern.Substring(++index, searchPattern.Length - index).Split(',')
                                           .Select(p => p.Trim().ToLowerInvariant()).ToArray();

                        if (!(subparts.Contains(serviceToken) || subparts.Contains(serviceName)) &&
                            (subparts.Contains("!" + serviceToken) || subparts.Contains("!" + serviceName) ||
                            !subparts.Any(s => s[0] == '!')))
                            continue;

                        searchPattern = searchPattern.Substring(0, index).Trim('\t');
                    }

                    Minimatcher miniMatcher = new Minimatcher(searchPattern, new Options { AllowWindowsPaths = true });

                    if (miniMatcher.IsMatch(sourcePath))
                        return true;
                }
            }

            return false;
        }

        private static string GetIgnoreFile(string startDir, string settingsFileName)
        {
            while (!File.Exists(Path.Combine(startDir, settingsFileName)))
            {
                startDir = Path.GetDirectoryName(startDir);

                if (String.IsNullOrEmpty(startDir))
                    break;
            }

            if (String.IsNullOrEmpty(startDir))
                startDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string fileName = Path.Combine(startDir, settingsFileName);

            if (!File.Exists(fileName))
                return null;

            return fileName;
        }
    }
}
