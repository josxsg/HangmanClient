using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HangmanClient
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _languageCode;

        public MainWindow()
        {
            InitializeComponent();
            _languageCode = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Login ventanaLogin = new Login();
            ventanaLogin.Show();
            this.Close();
        }

        private void btnLanguage_Click(object sender, RoutedEventArgs e)
        {
            popLanguage.IsOpen = true;
        }

        private void btnSpanish_Click(object sender, RoutedEventArgs e)
        {
            ChangueLanguage("es");
        }

        private void btnEnglish_Click(object sender, RoutedEventArgs e)
        {
            ChangueLanguage("en");
        }

        private void ChangueLanguage(string cultureCode)
        {
            popLanguage.IsOpen = false;

            if (_languageCode == cultureCode) return;

            CultureInfo newCulture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;

            MainWindow windowUpdate = new MainWindow();
            windowUpdate.Show();
            this.Close();
        }

    }
}
