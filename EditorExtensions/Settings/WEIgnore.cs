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

                    if (string.IsNullOrEmpty(searchPattern) || searchPattern.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    int index = searchPattern.LastIndexOf('\t');

                    if (index > 0)
                    {
                        string newPattern = searchPattern.Substring(0, index).Trim('\t');
                        string[] subparts = searchPattern.Substring(++index, searchPattern.Length - index).Split(',')
                                           .Select(p => p.Trim().ToLowerInvariant()).ToArray();

                        if (subparts.Contains(serviceToken) || subparts.Contains(serviceName))
                        {
                            if (newPattern[0] == '!' &&
                                new Minimatcher(newPattern.Substring(1), new Options { AllowWindowsPaths = true }).IsMatch(sourcePath))
                                return false;
                            else if (new Minimatcher(newPattern, new Options { AllowWindowsPaths = true }).IsMatch(sourcePath))
                                return true;
                        }
                        else if (subparts.Contains("!" + serviceToken) || subparts.Contains("!" + serviceName))
                        {
                            if (newPattern[0] == '!' &&
                                new Minimatcher(newPattern.Substring(1), new Options { AllowWindowsPaths = true }).IsMatch(sourcePath))
                                return true;
                            else if (new Minimatcher(newPattern, new Options { AllowWindowsPaths = true }).IsMatch(sourcePath))
                                return false;
                        }
                        else // The rule is not applicable on this service, continue checking other rules.
                            continue;

                        searchPattern = newPattern;
                    }

                    if (searchPattern[0] == '!' &&
                        new Minimatcher(searchPattern, new Options { AllowWindowsPaths = true }).IsMatch(sourcePath))
                        return false;
                    else if (new Minimatcher(searchPattern, new Options { AllowWindowsPaths = true }).IsMatch(sourcePath))
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
