using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Interaction logic for ImageDropDialog.xaml
    /// </summary>
    public partial class ImageDropFormattingDialog : Window
    {
        readonly ObservableCollection<ImageDropFormat> _formats = WESettings.Instance.Html.ImageDropFormats;
        readonly IList<string> _imagePaths;

        public ImageDropFormattingDialog(IEnumerable<string> imagePaths)
        {
            _imagePaths = imagePaths.ToList();

            InitializeComponent();
            FormatComboBox.ItemsSource = _formats;
            BindResources(0);

            if (_formats.Any())
                BuildMarkup(_formats.First().HtmlFormat);
        }

        public string ShowAsDialog()
        {
            if (this.ShowDialog().Value)
                return MarkupTextBlock.Text;

            return string.Empty;
        }

        private void BuildMarkup(string format)
        {
            StringBuilder markup = new StringBuilder();

            for (int i = 0; i < _imagePaths.Count; ++i)
            {
                markup.AppendFormat(
                    CultureInfo.CurrentCulture, 
                    format + Environment.NewLine, 
                    WebUtility.HtmlEncode(_imagePaths[i]), i
                );
            }

            // TODO: format markup?

            MarkupTextBlock.Text = markup.ToString();
        }

        public void BindResources(int selected)
        {
            FormatComboBox.SelectedIndex = selected;
        }

        public void AddFormat(ImageDropFormat format)
        {
            if (format == null)
                return;

            _formats.Add(format);
            SettingsStore.Save();

            BindResources(_formats.Count - 1);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            AddFormat(new AddFormat().ShowAsDialog());
        }

        private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BuildMarkup(FormatComboBox.SelectedValue.ToString());
        }

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}
