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
    /// Lógica de interacción para MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
        private string _username;
        private string _languageCode = "es";

        public MainMenu()
        {
            InitializeComponent();

            if (UserSession.Instance.IsLoggedIn)
            {
                lbUsername.Content = UserSession.Instance.CurrentUser.Username;
            }
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
            if (UserSession.Instance.IsLoggedIn)
            {
                var currentUser = UserSession.Instance.CurrentUser;

                txtProfName.Text = currentUser.Name;
                txtProfPaternalSurname.Text = currentUser.PaternalSurname;
                txtProfMaternalSurname.Text = currentUser.MaternalSurname;
                txtProfUsername.Text = currentUser.Username;
                txtProfEmail.Text = currentUser.Email;
                txtProfPhone.Text = currentUser.PhoneNumber;
            }

            popEditProfile.IsOpen = true;
        }

        private void btnCreateMatch_Click(object sender, RoutedEventArgs e)
        {
            CreateMatch createMatchWindow = new CreateMatch(_languageCode);
            createMatchWindow.Show();
            this.Close();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            JoinMatch joinMatchWindow = new JoinMatch(_languageCode);
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

        private async void btnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProfName.Text) ||
                string.IsNullOrWhiteSpace(txtProfPaternalSurname.Text) ||
                string.IsNullOrWhiteSpace(txtProfUsername.Text))
            {
                MessageBox.Show("El nombre, apellido paterno y nombre de usuario son obligatorios.", "Campos vacíos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnSaveProfile.IsEnabled = false;

            try
            {
                UserDTO updatedUser = new UserDTO
                {
                    UserId = UserSession.Instance.CurrentUser.UserId,
                    Name = txtProfName.Text.Trim(),
                    PaternalSurname = txtProfPaternalSurname.Text.Trim(),
                    MaternalSurname = txtProfMaternalSurname.Text?.Trim() ?? string.Empty,
                    Username = txtProfUsername.Text.Trim(),
                    Email = txtProfEmail.Text,
                    PhoneNumber = txtProfPhone.Text.Trim(),
                    BirthDate = UserSession.Instance.CurrentUser.BirthDate
                };

                using (var client = new AccountServiceClient())
                {
                    bool success = await client.UpdateUserProfileAsync(updatedUser);

                    if (success)
                    {
                        UserSession.Instance.CurrentUser = updatedUser;

                        lbUsername.Content = updatedUser.Username;

                        MessageBox.Show("Tus datos han sido actualizados con éxito.", "Perfil actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
                        popEditProfile.IsOpen = false;
                    }
                    else
                    {
                        MessageBox.Show("No se pudo actualizar. Es probable que el nombre de usuario ya esté ocupado por otro jugador.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión al guardar los datos: {ex.Message}", "Error de Servidor", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSaveProfile.IsEnabled = true;
            }
        }
    }
}
