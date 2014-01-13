using System.Collections.Generic;
using System.Net;
using System.Windows;

namespace MadsKristensen.EditorExtensions
{
    /// <summary>
    /// Interaction logic for AddFormat.xaml
    /// </summary>
    public partial class AddFormat : Window
    {
        public AddFormat()
        {
            InitializeComponent();
        }

        public ImageDropFormat ShowAsDialog()
        {
            if (this.ShowDialog().Value)
                return new ImageDropFormat(FormatName.Text, Markup.Text);

            return null;
        }

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FormatName.Text))
                return;

            DialogResult = true;
            this.Close();
        }
    }
}
