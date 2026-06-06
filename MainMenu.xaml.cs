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
        public MainMenu()
        {
            InitializeComponent();
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

        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {

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
