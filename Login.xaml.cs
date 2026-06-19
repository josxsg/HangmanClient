using HangmanClient.AccountServiceRef;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private async void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            if (!IsFormValid()) return;

            btnLogIn.IsEnabled = false;

            string usernameInput = txtBlockUsername.Text.Trim();

            string hashedPassword = ComputeSha256Hash(pwsBoxPassword.Password);

            var authenticatedUser = await RequestLoginAuthenticationAsync(usernameInput, hashedPassword);

            if (authenticatedUser != null)
            {
                if (authenticatedUser.Username != usernameInput)
                {
                    MessageBox.Show(Properties.Resources.mbLetterCase,
                                    Properties.Resources.mbIncorrectUser, MessageBoxButton.OK, MessageBoxImage.Warning);
                    btnLogIn.IsEnabled = true;
                    txtBlockUsername.Focus();
                    return;
                }

                UserSession.Instance.CurrentUser = authenticatedUser;

                string message = string.Format(Properties.Resources.mbWelcomeBack, authenticatedUser.Name);
                MessageBox.Show(message, Properties.Resources.mbSuccess, MessageBoxButton.OK, MessageBoxImage.Information);

                RedirectToMainMenu();
            }
            else
            {
                MessageBox.Show(Properties.Resources.mbUserOrPswIncorrect, Properties.Resources.mbAuthenticationError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnLogIn.IsEnabled = true;
            }
        }

        private void btnSignIn_Click(object sender, RoutedEventArgs e)
        {
            RedirectToSignUp();
        }

        private string ComputeSha256Hash(string rawPassword)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawPassword));
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private bool IsFormValid()
        {
            if (string.IsNullOrWhiteSpace(txtBlockUsername.Text) || string.IsNullOrWhiteSpace(pwsBoxPassword.Password))
            {
                MessageBox.Show(Properties.Resources.mbNullUserPsw, Properties.Resources.mbNullSpaces,
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

        private void RedirectToSignUp()
        {
            SignUp signUpWindow = new SignUp();
            signUpWindow.Show();
            this.Close();
        }

        private async Task<UserDTO> RequestLoginAuthenticationAsync(string username, string hashedPassword)
        {
            try
            {
                using (var client = new AccountServiceClient())
                {
                    return await client.LoginAsync(username, hashedPassword);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(Properties.Resources.mbSrvrNetError, ex.Message);
                MessageBox.Show(errorMessage, Properties.Resources.mbNetworkError,
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void txtBlockUsername_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}