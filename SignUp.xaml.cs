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
    public partial class SignUp : Window
    {
        public SignUp()
        {
            InitializeComponent();
        }

        private async void btnSignUp_Click(object sender, RoutedEventArgs e)
        {
            if (!IsFormValid()) return;

            btnSignUp.IsEnabled = false;
            var nuevoUsuario = BuildUserDTO();
            string password = pswBoxPassword.Password;

            bool registroExitoso = await RequestUserRegistrationAsync(nuevoUsuario, password);

            if (registroExitoso)
            {
                MessageBox.Show("¡Registro completado con éxito!", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                RedirectToLogin();
            }
            else
            {
                btnSignUp.IsEnabled = true;
            }
        }

        private async Task<bool> RequestUserRegistrationAsync(UserDTO usuario, string password)
        {
            try
            {
                using (var client = new AccountServiceClient())
                {
                    bool resultado = await client.RegisterUserAsync(usuario, password);

                    if (!resultado)
                    {
                        MessageBox.Show("El correo electrónico o nombre de usuario ya existen.", "Error de Validación",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return resultado;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión con el servidor de cuentas: {ex.Message}", "Error de Red",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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
                MessageBox.Show("Por favor, llena todos los campos obligatorios.", "Campos vacíos",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

       
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            Login ventanaLogin = new Login();
            ventanaLogin.Show();
            this.Close();
        }
    }
}