using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MadsKristensen.EditorExtensions
{
    public partial class BrowserSelector : Window
    {
        public BrowserSelector()
        {
            InitializeComponent();
            ReadBrowsers();
            CheckedChanged(null, new RoutedEventArgs());

            ToggleEnabled(cbChrome);
            ToggleEnabled(cbFirefox);
            ToggleEnabled(cbIE);
            ToggleEnabled(cbOpera);
            ToggleEnabled(cbSafari);
        }

        private void ToggleEnabled(CheckBox cb)
        {
            cb.Checked += CheckedChanged;
            cb.Unchecked += CheckedChanged;
        }

        void CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!cbChrome.IsChecked.Value &&
                !cbFirefox.IsChecked.Value &&
                !cbIE.IsChecked.Value &&
                !cbOpera.IsChecked.Value &&
                !cbSafari.IsChecked.Value)
            {
                btnOk.IsEnabled = false;
            }

            btnOk.IsEnabled = true;
        }

        public IEnumerable<string> Browsers()
        {
            if (cbChrome.IsChecked.Value)
                yield return "C";

            if (cbFirefox.IsChecked.Value)
                yield return "FF";

            if (cbIE.IsChecked.Value)
                yield return "IE";

            if (cbOpera.IsChecked.Value)
                yield return "O";

            if (cbSafari.IsChecked.Value)
                yield return "S";
        }

        private void ReadBrowsers()
        {
            var browsers = BrowserStore.Browsers;

            if (browsers.Any())
            {
                foreach (string browser in browsers)
                {
                    switch (browser)
                    {
                        case "C":
                            cbChrome.IsChecked = true;
                            break;

                        case "FF":
                            cbFirefox.IsChecked = true;
                            break;

                        case "IE":
                            cbIE.IsChecked = true;
                            break;

                        case "O":
                            cbOpera.IsChecked = true;
                            break;

                        case "S":
                            cbSafari.IsChecked = true;
                            break;
                    }
                }

            }
            else
            {
                cbChrome.IsChecked = true;
                cbFirefox.IsChecked = true;
                cbIE.IsChecked = true;
                cbOpera.IsChecked = true;
                cbSafari.IsChecked = true;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            BrowserStore.SaveBrowsers(this.Browsers());
            this.Close();
        }
    }
}
