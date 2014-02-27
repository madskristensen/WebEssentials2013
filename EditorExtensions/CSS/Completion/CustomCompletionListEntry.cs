using System;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class CustomCompletionListEntry : ICssCompletionListEntry
    {
        private string _insertion;

        public CustomCompletionListEntry(string name, string insertion)
        {
            this.DisplayText = name;
            _insertion = insertion;
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

        public int SortingPriority { get { return 0; } }


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

        public System.Windows.Media.ImageSource Icon
        {
            get { return null; }
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
