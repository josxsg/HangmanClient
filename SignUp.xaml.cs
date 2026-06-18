using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
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
            var newUser = BuildUserDTO();
            string hashedPassword = ComputeSha256Hash(pswPassword.Password);

            bool isRegistrationSuccessful = await RequestUserRegistrationAsync(newUser, hashedPassword);

            if (isRegistrationSuccessful)
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

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            RedirectToLogin();
        }

        private UserDTO BuildUserDTO() => new UserDTO
        {
            Name = txtFirstName.Text.Trim(),
            PaternalSurname = txtPaternalSurname.Text.Trim(),
            MaternalSurname = txtMaternalSurname.Text.Trim(),
            Username = txtUsername.Text.Trim(),
            Email = txtEmail.Text.Trim(),
            BirthDate = dpBirthDate.SelectedDate ?? DateTime.Now,
            PhoneNumber = txtPhoneNumber.Text.Trim()
        };

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
            if (!ValidateRequiredFields()) return false;
            if (!ValidateCharacterLengths()) return false;
            if (!ValidatePhoneNumber()) return false;
            if (!ValidateEmailFormat()) return false;
            if (!ValidateBirthDateRange()) return false;
            if (!ValidatePasswordStrength()) return false;

            return true;
        }

        private async Task<bool> RequestUserRegistrationAsync(UserDTO user, string password)
        {
            try
            {
                using (var client = new AccountServiceClient())
                {
                    bool result = await client.RegisterUserAsync(user, password);

                    if (!result)
                    {
                        MessageBox.Show("El correo electrónico o nombre de usuario ya existen.", "Error de Validación",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión con el servidor de cuentas: {ex.Message}", "Error de Red",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        private bool ValidateBirthDateRange()
        {
            DateTime birthDate = dpBirthDate.SelectedDate.Value;
            DateTime today = DateTime.Today;

            DateTime minAllowedDate = today.AddYears(-100);
            DateTime maxAllowedDate = today.AddYears(-6);

            if (birthDate < minAllowedDate || birthDate > maxAllowedDate)
            {
                MessageBox.Show("La edad permitida para registrarse debe estar entre los 6 y los 100 años.", "Fecha no admitida",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpBirthDate.Focus();
                return false;
            }
            return true;
        }

        private bool ValidateCharacterLengths()
        {
            TextBox[] textFields = { txtFirstName, txtPaternalSurname, txtMaternalSurname, txtUsername, txtEmail, txtPhoneNumber };

            foreach (var field in textFields)
            {
                if (field.Text.Trim().Length > 25)
                {
                    MessageBox.Show("Los campos de texto no deben superar los 25 caracteres.", "Límite superado",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    field.Focus();
                    return false;
                }
            }
            return true;
        }

        

        private bool ValidateEmailFormat()
        {
            string email = txtEmail.Text.Trim();
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            if (!Regex.IsMatch(email, emailPattern))
            {
                MessageBox.Show("Por favor, ingresa un correo electrónico válido (ejemplo@dominio.com).", "Correo inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }
            return true;
        }

        private bool ValidatePasswordStrength()
        {
            string password = pswPassword.Password;

            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";

            if (!Regex.IsMatch(password, passwordPattern))
            {
                MessageBox.Show("La contraseña es muy débil. Debe tener al menos 8 caracteres, incluir una letra mayúscula, una minúscula y un número.",
                    "Contraseña Insegura", MessageBoxButton.OK, MessageBoxImage.Warning);
                pswPassword.Focus();
                return false;
            }
            return true;
        }

        private bool ValidatePhoneNumber()
        {
            string phone = txtPhoneNumber.Text.Trim();
            if (!Regex.IsMatch(phone, @"^\d{10}$"))
            {
                MessageBox.Show("El teléfono debe contener exactamente 10 dígitos numéricos.", "Teléfono inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPhoneNumber.Focus();
                return false;
            }
            return true;
        }

        private bool ValidateRequiredFields()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtPaternalSurname.Text) ||
                string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtEmail.Text) ||
                string.IsNullOrWhiteSpace(txtPhoneNumber.Text) ||
                !dpBirthDate.SelectedDate.HasValue ||
                string.IsNullOrWhiteSpace(pswPassword.Password))
            {
                MessageBox.Show("Por favor, llena todos los campos obligatorios.", "Campos vacíos",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void RedirectToLogin()
        {
            Login loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }
    }
}