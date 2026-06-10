using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HangmanClient
{
    /// <summary>
    /// Lógica de interacción para MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
        private string _username;
        private string _languageCode = "es";

        public MainMenu()
        {
            InitializeComponent();
        }

        public MainMenu(string username)
        {
            InitializeComponent();

            _username = username;

            lbUsername.Content = _username;
        }

        private void btnMyPoints_Click(object sender, RoutedEventArgs e)
        {
            popMyPoints.IsOpen = true;
        }

        private void btnLanguage_Click(object sender, RoutedEventArgs e)
        {
            popLanguage.IsOpen = true;
        }

        private void btnEditProfile_Click(object sender, RoutedEventArgs e)
        {
            popEditProfile.IsOpen = true;
        }

        private void btnCreateMatch_Click(object sender, RoutedEventArgs e)
        {
            CreateMatch createMatchWindow = new CreateMatch(_username, _languageCode);
            createMatchWindow.Show();
            this.Close();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            JoinMatch joinMatchWindow = new JoinMatch(_username, _languageCode);
            joinMatchWindow.Show();
            this.Close();
        }

        private void btnSpanish_Click(object sender, RoutedEventArgs e)
        {
            popLanguage.IsOpen = false; 
        }

        private void btnEnglish_Click(object sender, RoutedEventArgs e)
        {
            popLanguage.IsOpen = false; 
        }
    }
}
