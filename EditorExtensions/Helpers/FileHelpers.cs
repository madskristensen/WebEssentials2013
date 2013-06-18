using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    public static class FileHelpers
    {
        public static string ConvertToBase64(string fileName)
        {
            string format = "data:{0};base64,{1}";
            byte[] buffer = File.ReadAllBytes(fileName);
            string extension = Path.GetExtension(fileName).Substring(1);
            string contentType = GetMimeType(extension);

            return string.Format(CultureInfo.InvariantCulture, format, contentType, Convert.ToBase64String(buffer));
        }

        private static string GetMimeType(string extension)
        {
            switch (extension)
            {
                case "png":
                case "jpg":
                case "jpeg":
                case "gif":
                    return "image/" + extension;

                case "woff":
                    return "font/x-woff";

                case "otf":
                    return "font/otf";

                case "eot":
                    return "application/vnd.ms-fontobject";

                case "ttf":
                    return "application/octet-stream";

                default:
                    return "text/plain";
            }
        }

        public static string RelativePath(string absPath, string relTo)
        {
            string[] absDirs = absPath.Split('\\');
            string[] relDirs = relTo.Split('\\');

            // Get the shortest of the two paths
            int len = absDirs.Length < relDirs.Length ? absDirs.Length :
            relDirs.Length;

            // Use to determine where in the loop we exited
            int lastCommonRoot = -1;
            int index;

            // Find common root
            for (index = 0; index < len; index++)
            {
                if (absDirs[index] == relDirs[index]) lastCommonRoot = index;
                else break;
            }

            // If we didn't find a common prefix then throw
            if (lastCommonRoot == -1)
            {
                return relTo;
            }

            // Build up the relative path
            StringBuilder relativePath = new StringBuilder();

            // Add on the ..
            for (index = lastCommonRoot + 2; index < absDirs.Length; index++)
            {
                if (absDirs[index].Length > 0) relativePath.Append("..\\");
            }

            // Add on the folders
            for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++)
            {
                relativePath.Append(relDirs[index] + "\\");
            }
            relativePath.Append(relDirs[relDirs.Length - 1]);

            return relativePath.ToString().Replace("\\", "/");
        }
    }
}
