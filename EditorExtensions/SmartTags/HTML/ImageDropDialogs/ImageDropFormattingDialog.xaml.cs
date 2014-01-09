using System;
using System.Collections.Generic;
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
        private static IEnumerable<KeyValuePair<string, string>> _formatsList = Settings.LoadImageDropFormats();//(IEnumerable<KeyValuePair<string, string>>)Settings.GetValue(WESettings.Keys.ImageDropFormats);
        private IEnumerable<string> _imagePaths;

        public ImageDropFormattingDialog(IEnumerable<string> imagePaths)
        {
            _imagePaths = imagePaths;

            InitializeComponent();
            BindResources(0);
            BuildMarkup(_formatsList.First().Value);
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

            // Needs to be converted to Linq?
            var images = _imagePaths.ToArray();

            for (int i = 0; i < images.Length; ++i)
            {
                markup.Append(WebUtility.HtmlDecode(String.Format(CultureInfo.CurrentCulture, format, images[i], i) + Environment.NewLine));
            }

            // TODO: format markup?

            MarkupTextBlock.Text = markup.ToString();
        }

        public void BindResources(int selected)
        {
            FormatComboBox.ItemsSource = _formatsList;
            FormatComboBox.SelectedIndex = selected;
        }

        /// <summary>
        /// Serialize back to settings xml and refresh list.
        /// </summary>
        public void UpdateResources(KeyValuePair<string, string>? format)
        {
            if (format == null)
                return;

            _formatsList = _formatsList.Concat(new[] { format.GetValueOrDefault() });

            Settings.UpdateImageDropFormats(_formatsList);
            BindResources(_formatsList.Count() - 1);
            FormatComboBox.SelectedIndex = _formatsList.Count() - 1;
            BuildMarkup(FormatComboBox.SelectedValue.ToString());
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            UpdateResources(new AddFormat().ShowAsDialog());
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
