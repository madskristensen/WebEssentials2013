using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class UrlPickerCompletionListEntry : ICssCompletionListEntry
    {
        private readonly string _name;

        public UrlPickerCompletionListEntry(FileSystemInfo file)
        {
            _name = file.Name;

            Description = file.FullName + Environment.NewLine;

            var dir = file as DirectoryInfo;
            if (dir != null)
            {
                IsFolder = true;
                Description += dir.EnumerateFiles().Count() + " files";
            }
            else
            {
                Description += ToSizeString(((FileInfo)file).Length);
            }
        }

        // Based on https://github.com/flagbug/Rareform/blob/master/Rareform/Rareform/Extensions/LongExtensions.cs
        static readonly string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        public static string ToSizeString(long size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException("size");

            int i;
            double result = size;

            for (i = 0; (int)(size / 1024) > 0; i++, size /= 1024)
            {
                result = size / 1024.0;
            }

            // Bytes shouldn't have decimal places
            string format = i == 0 ? "{0} {1}" : "{0:0.00} {1}";

            return String.Format(CultureInfo.CurrentCulture, format, result, suffixes[i]);
        }
        public bool AllowQuotedString
        {
            get { return false; }
        }

        public string Description { get; private set; }

        public string DisplayText
        {
            get { return _name; }
        }

        public string GetSyntax(Version version)
        {
            return string.Empty;
        }

        public int SortingPriority
        {
            get { return IsFolder ? 1 : 0; }
        }

        public bool IsBuilder
        {
            get { return false; }
        }

        public StandardGlyphGroup StandardGlyph
        {
            get { return IsFolder ? StandardGlyphGroup.GlyphClosedFolder : StandardGlyphGroup.GlyphBscFile; }
        }

        public string GetAttribute(string name)
        {
            return string.Empty;
        }

        public string GetInsertionText(CssTextSource textSource, ITrackingSpan typingSpan)
        {
            string text = HttpUtility.UrlPathEncode(DisplayText);
            if (IsFolder)
                text += "/";
            return text;
        }

        public string GetVersionedAttribute(string name, Version version)
        {
            return GetAttribute(name);
        }

        private bool IsFolder { get; set; }

        public bool IsSupported(BrowserVersion browser)
        {
            return true;
        }

        public bool IsSupported(Version cssVersion)
        {
            return true;
        }
    }
}
