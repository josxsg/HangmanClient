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
                MessageBox.Show(Properties.Resources.mbRegisterSuccess, Properties.Resources.mbSuccess,
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

        private void pswPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            lbPasswordCounter.Text = $"{pswPassword.Password.Length}/15";
        }

        private void txtFirstName_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbFirstNameCounter.Text = $"{txtFirstName.Text.Length}/25";
        }

        private void txtPaternalSurname_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbPaternalSurnameCounter.Text = $"{txtPaternalSurname.Text.Length}/25";
        }

        private void txtMaternalSurname_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbMaternalSurnameCounter.Text = $"{txtMaternalSurname.Text.Length}/25";
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbUsernameCounter.Text = $"{txtUsername.Text.Length}/25";
        }

        private void txtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbEmailCounter.Text = $"{txtEmail.Text.Length}/25";
        }

        private void txtPhoneNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbPhoneNumberCounter.Text = $"{txtPhoneNumber.Text.Length}/25";
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

        private static string ComputeSha256Hash(string rawPassword)
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
            if (!ValidatePhoneNumber()) return false;
            if (!ValidateEmailFormat()) return false;
            if (!ValidateBirthDateRange()) return false;
            if (!ValidatePasswordStrength()) return false;

            return true;
        }

        private static async Task<bool> RequestUserRegistrationAsync(UserDTO user, string password)
        {
            try
            {
                using (var client = new AccountServiceClient())
                {
                    bool result = await client.RegisterUserAsync(user, password);

                    if (!result)
                    {
                        MessageBox.Show(Properties.Resources.mbEmailOrUsernameExists, Properties.Resources.mbValidationError,
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(Properties.Resources.mbAccountSrvrNetError, ex.Message);
                MessageBox.Show(errorMessage, Properties.Resources.mbNetworkError,
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
                MessageBox.Show(Properties.Resources.mbBirthDateRange, Properties.Resources.mbInvalidDate,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpBirthDate.Focus();
                return false;
            }
            return true;
        }

        private bool ValidateEmailFormat()
        {
            string email = txtEmail.Text.Trim();
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            try
            {
                if (!Regex.IsMatch(email, emailPattern, RegexOptions.None, TimeSpan.FromMilliseconds(200)))
                {
                    MessageBox.Show(Properties.Resources.mbInvalidEmail, Properties.Resources.mbInEmail,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    return false;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            return true;
        }

        private bool ValidatePasswordStrength()
        {
            string password = pswPassword.Password;
            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";

            try
            {
                if (!Regex.IsMatch(password, passwordPattern, RegexOptions.None, TimeSpan.FromMilliseconds(200)))
                {
                    MessageBox.Show(Properties.Resources.mbPswWeak,
                        Properties.Resources.mbPswInsecure, MessageBoxButton.OK, MessageBoxImage.Warning);
                    pswPassword.Focus();
                    return false;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            return true;
        }

        private bool ValidatePhoneNumber()
        {
            string phone = txtPhoneNumber.Text.Trim();

            try
            {
                if (!Regex.IsMatch(phone, @"^\d{10}$", RegexOptions.None, TimeSpan.FromMilliseconds(200)))
                {
                    MessageBox.Show(Properties.Resources.mbPhoneDigits, Properties.Resources.mbInvalidPhone,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPhoneNumber.Focus();
                    return false;
                }
            }
            catch (RegexMatchTimeoutException)
            {
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
                MessageBox.Show(Properties.Resources.mbNullOb, Properties.Resources.mbNullSpaces,
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