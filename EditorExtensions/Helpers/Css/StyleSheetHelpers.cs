using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;

namespace MadsKristensen.EditorExtensions
{
    public static class StyleSheetHelpers
    {
        public static IEnumerable<string> GetAllStyleSheets(string searchFrom, IEnumerable<string> allowedExtensions)
        {
            var project = ProjectHelpers.GetProject(searchFrom);
            var projectPath = project.Properties.Item("FullPath").Value.ToString();
            var projectUri = new Uri(projectPath, UriKind.Absolute);
            var projectDir = Path.GetDirectoryName(projectPath);

            if (projectDir == null)
                return Enumerable.Empty<string>();

            return allowedExtensions
                .SelectMany(e => Directory.EnumerateFiles(projectDir, "*" + e, SearchOption.AllDirectories))
                .Select(f => GetStyleSheetFileForUrl(f, project, projectUri))
                .Where(f => f != null);
        }

        public static string GetStyleSheetFileForUrl(string location, Project project, Uri projectUri = null)
        {
            if (projectUri == null)
            {
                //TODO: This needs to expand bundles, convert urls to local file names, and move from .min.css files to .css files where applicable
                //NOTE: Project parameter here is for the discovery of linked files, ones that might exist outside of the project structure
                var projectPath = project.Properties.Item("FullPath").Value.ToString();
                projectUri = new Uri(projectPath, UriKind.Absolute);
            }

            if (location == null)
            {
                return null;
            }

            var locationUri = new Uri(location, UriKind.RelativeOrAbsolute);

            //No absolute paths, unless they map into the same project
            if (locationUri.IsAbsoluteUri)
            {
                if (projectUri.IsBaseOf(locationUri))
                {
                    locationUri = projectUri.MakeRelativeUri(locationUri);
                }
                else
                {
                    //TODO: Fix this, it'll only work if the site is at the root of the server as is
                    locationUri = new Uri(locationUri.LocalPath, UriKind.Relative);
                }

                if (locationUri.IsAbsoluteUri)
                {
                    return null;
                }
            }

            var locationUrl = locationUri.ToString().TrimStart('/').ToLowerInvariant();

            //Hoist .min.css -> .css
            if (locationUrl.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase))
            {
                locationUrl = locationUrl.Substring(0, locationUrl.Length - 8) + ".css";
            }

            locationUri = new Uri(locationUrl, UriKind.Relative);
            string filePath;

            try
            {
                Uri realLocation;
                if (Uri.TryCreate(projectUri, locationUri, out realLocation) && File.Exists(realLocation.LocalPath))
                {
                    //Try to move from .css -> .less
                    var lessFile = Path.ChangeExtension(realLocation.LocalPath, ".less");

                    if (File.Exists(lessFile))
                    {
                        locationUri = new Uri(lessFile, UriKind.Relative);
                        Uri.TryCreate(projectUri, locationUri, out realLocation);
                    }

                    filePath = realLocation.LocalPath;
                }
                else
                {
                    //Try to move from .min.css -> .less
                    var lessFile = Path.ChangeExtension(realLocation.LocalPath, ".less");

                    if (!File.Exists(lessFile))
                    {
                        return null;
                    }

                    locationUri = new Uri(lessFile, UriKind.Relative);
                    Uri.TryCreate(projectUri, locationUri, out realLocation);
                    filePath = realLocation.LocalPath;
                }
            }
            catch (IOException)
            {
                return null;
            }

            return filePath;
        }
    }
}
