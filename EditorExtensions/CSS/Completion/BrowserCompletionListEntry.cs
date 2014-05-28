using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class BrowserCompletionListEntry : ICssCompletionListEntry
    {
        private string _insertion;
        private ImageSource _icon;

        public BrowserCompletionListEntry(string name, string browserName)
        {
            this.DisplayText = name + " (from " + browserName + ")";
            _insertion = name;
            SetIcon(browserName.ToLowerInvariant());
        }

        private void SetIcon(string browserName)
        {
            if (browserName.Contains("internet explorer"))
                _icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/Browsers/ie.png", UriKind.RelativeOrAbsolute));
            else if (browserName.Contains("firefox"))
                _icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/Browsers/ff.png", UriKind.RelativeOrAbsolute));
            else if (browserName.Contains("opera"))
                _icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/Browsers/o.png", UriKind.RelativeOrAbsolute));
            else if (browserName.Contains("chrome"))
                _icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/Browsers/c.png", UriKind.RelativeOrAbsolute));
            else if (browserName.Contains("safari"))
                _icon = BitmapFrame.Create(new Uri("pack://application:,,,/WebEssentials2013;component/Resources/Images/Browsers/s.png", UriKind.RelativeOrAbsolute));
            else
                _icon = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupEnumMember, StandardGlyphItem.GlyphItemPublic);
        }

        public string Description { get; set; }

        public string DisplayText { get; set; }

        public string GetSyntax(Version version)
        {
            return string.Empty;
        }

        public string GetAttribute(string name)
        {
            return string.Empty;
        }

        public string GetInsertionText(CssTextSource textSource, ITrackingSpan typingSpan)
        {
            return _insertion;
        }

        public string GetVersionedAttribute(string name, Version version)
        {
            return GetAttribute(name);
        }

        public bool AllowQuotedString
        {
            get { return false; }
        }

        public bool IsBuilder
        {
            get { return false; }
        }

        public int SortingPriority
        {
            get { return -1; }
        }


        public bool IsSupported(BrowserVersion browser)
        {
            return true;
        }

        public bool IsSupported(Version cssVersion)
        {
            return true;
        }

        public ITrackingSpan ApplicableTo
        {
            get { return null; }
        }

        public Microsoft.Web.Editor.Intellisense.CompletionEntryFilterTypes FilterType
        {
            get { return Microsoft.Web.Editor.Intellisense.CompletionEntryFilterTypes.AlwaysVisible; }
        }

        public ImageSource Icon
        {
            get { return _icon; }
        }

        public bool IsCommitChar(char typedCharacter)
        {
            return false;
        }

        public bool IsMuteCharacter(char typedCharacter)
        {
            return false;
        }

        public bool RetriggerIntellisense
        {
            get { return false; }
        }
    }
}
