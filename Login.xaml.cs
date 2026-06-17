using HangmanClient.AccountServiceRef;
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
    /// Lógica de interacción para Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void txtBlockUsername_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            if (!IsFormValid()) return;

            btnLogIn.IsEnabled = false;

            try
            {
                using (var client = new AccountServiceClient())
                {
                    var loggedInUser = await client.LoginAsync(txtBlockUsername.Text.Trim(), pwsBoxPassword.Password);

                    if (loggedInUser != null)
                    {
                        UserSession.Instance.CurrentUser = loggedInUser;

                        MessageBox.Show($"¡Bienvenido de nuevo, {loggedInUser.Name}!", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        RedirectToMainMenu();
                    }
                    else
                    {
                        MessageBox.Show("El usuario o la contraseña son incorrectos.", "Error de autenticación",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        btnLogIn.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo conectar con el servidor: {ex.Message}", "Error de red",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnLogIn.IsEnabled = true;
            }
        }

        private void btnSignIn_Click(object sender, RoutedEventArgs e)
        {
            SignUp signUpWindow = new SignUp();
            signUpWindow.Show();
            this.Close();
        }

        private bool IsFormValid()
        {
            if (string.IsNullOrWhiteSpace(txtBlockUsername.Text) || string.IsNullOrWhiteSpace(pwsBoxPassword.Password))
            {
                MessageBox.Show("Por favor, introduce tu usuario y contraseña.", "Campos vacíos", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void RedirectToMainMenu()
        {
            MainMenu mainMenuWindow = new MainMenu(); 
            mainMenuWindow.Show();
            this.Close();
        }
    }
}