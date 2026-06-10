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
using HangmanClient.AccountServiceRef;

namespace HangmanClient
{
    /// <summary>
    /// Lógica de interacción para SignUp.xaml
    /// </summary>
    public partial class SignUp : Window
    {
        public SignUp()
        {
            InitializeComponent();
        }

        private void txtName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtSecondName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtBoxEmail_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async void btnSignUp_Click(object sender, RoutedEventArgs e)
        {
            if (!IsFormValid()) return;

            btnSignUp.IsEnabled = false;

            try
            {
                var nuevoUsuario = BuildUserDTO();

                using (var client = new AccountServiceClient())
                {
                    bool registroExitoso = await client.RegisterUserAsync(nuevoUsuario, pswBoxPassword.Password);

                    if (registroExitoso)
                    {
                        MessageBox.Show("¡Registro completado con éxito!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        RedirectToLogin();
                    }
                    else
                    {
                        MessageBox.Show("El correo electrónico o nombre de usuario ya existen.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        btnSignUp.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error de red", MessageBoxButton.OK, MessageBoxImage.Error);
                btnSignUp.IsEnabled = true;
            }
        }

        private bool IsFormValid()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtSecondName.Text) ||
                string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtBoxEmail.Text) ||
                string.IsNullOrWhiteSpace(pswBoxPassword.Password))
            {
                MessageBox.Show("Por favor, llena todos los campos obligatorios.", "Campos vacíos", MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private UserDTO BuildUserDTO() => new UserDTO
        {
            Name = txtName.Text.Trim(),
            PaternalSurname = txtSecondName.Text.Trim(),
            MaternalSurname = string.Empty,
            Username = txtUsername.Text.Trim(),
            Email = txtBoxEmail.Text.Trim(),
            BirthDate = DateTime.Now.AddYears(-18),
            PhoneNumber = "2280000000"
        };

        private void RedirectToLogin()
        {
            Login loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }
    }
}
