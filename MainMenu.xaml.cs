using HangmanClient.AccountServiceRef;
using HangmanClient.ScoreServiceRef;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        private string _languageCode;

        public MainMenu()
        {
            InitializeComponent();
            _languageCode = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

            if (UserSession.Instance.IsLoggedIn)
            {
                lbUsername.Content = UserSession.Instance.CurrentUser.Username;
            }
        }



        private async void btnMyPoints_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.IsLoggedIn) return;

            try
            {
                btnMyPoints.IsEnabled = false;

                using (var client = new ScoreServiceClient())
                {
                    int userId = UserSession.Instance.CurrentUser.UserId;

                    var scoreTask = client.GetPlayerScoreAsync(userId);
                    var historyTask = client.GetMatchHistoryAsync(userId);

                    await Task.WhenAll(scoreTask, historyTask);

                    var score = scoreTask.Result;
                    var history = historyTask.Result;

                    lblTotalPoints.Content = $"{score.TotalScore} pts";
                    lblTotalPoints.Foreground = score.TotalScore >= 0
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32")) 
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828")); 

    
                    var historyViewList = history.Select(h =>
                    {
                        string resText = "";
                        string resColor = "#2E7D32"; 

                        switch (h.Result)
                        {
                            case "Ganada":
                                resText = string.Format(Properties.Resources.lbStatusWin, h.Points);
                                resColor = "#2E7D32"; 
                                break;
                            case "Perdida":
                                resText = string.Format(Properties.Resources.lbStatusLost, h.Points); 
                                resColor = "#C62828"; 
                                break;
                            case "Abandonada":
                                resText = string.Format(Properties.Resources.lbStatusAbandoned, h.Points);
                                resColor = "#C62828"; 
                                break;
                            default:
                                resText = h.Result;
                                break;
                        }

                        return new
                        {
                            MatchIdText = $"ID: {h.MatchId}",
                            DateText = h.Date.ToString("dd/MM/yyyy"),
                            RivalText = string.Format(Properties.Resources.lbOpponent, h.RivalUsername),
                            WordText = string.Format(Properties.Resources.lbWord, h.RivalUsername),
                            ResultText = resText,
                            ResultColor = resColor 
                        };
                    }).ToList();

                    icMatchHistory.ItemsSource = historyViewList;
                }

                popMyPoints.IsOpen = true;
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(Properties.Resources.mbHistoryLoadError, ex.Message);
                MessageBox.Show(errorMessage, Properties.Resources.mbNetworkError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnMyPoints.IsEnabled = true;
            }
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

        private bool IsProfileFormValid()
        {
            if (!ValidateRequiredProfileFields()) return false;
            if (!ValidateProfilePhoneNumber()) return false;

            return true;
        }

        private bool ValidateRequiredProfileFields()
        {
            if (string.IsNullOrWhiteSpace(txtProfName.Text) ||
                string.IsNullOrWhiteSpace(txtProfPaternalSurname.Text) ||
                string.IsNullOrWhiteSpace(txtProfUsername.Text) ||
                string.IsNullOrWhiteSpace(txtProfPhone.Text))
            {
                MessageBox.Show(Properties.Resources.mbNullOb, Properties.Resources.mbNullSpaces,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private bool ValidateProfilePhoneNumber()
        {
            string phone = txtProfPhone.Text.Trim();
            try
            {
                if (!Regex.IsMatch(phone, @"^\d{10}$", RegexOptions.None, TimeSpan.FromMilliseconds(200)))
                {
                    MessageBox.Show(Properties.Resources.mbPhoneDigits, Properties.Resources.mbInvalidPhone,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtProfPhone.Focus();
                    return false;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            return true;
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

            MainMenu menuActualizado = new MainMenu();
            menuActualizado.Show();
            this.Close();
        }

        private async void btnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!IsProfileFormValid()) return;

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
                        MessageBox.Show(Properties.Resources.mbUpdateSuccess, Properties.Resources.mbProfileUpdated,
                             MessageBoxButton.OK, MessageBoxImage.Information);
                        popEditProfile.IsOpen = false;
                    }
                    else
                    {
                        MessageBox.Show(Properties.Resources.mbUpdateError, Properties.Resources.mbError,
                             MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(Properties.Resources.mbSavingConnError, ex.Message);
                MessageBox.Show(errorMessage, Properties.Resources.mbServerError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSaveProfile.IsEnabled = true;
            }
        }
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(Properties.Resources.mbConfirmLogout, Properties.Resources.mbLogout, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                UserSession.Instance.Logout();

                Login loginWindow = new Login();
                loginWindow.Show();

                this.Close();
            }
        }

        private void txtProfName_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbProfNameCounter.Text = $"{txtProfName.Text.Length}/25";
        }

        private void txtProfPaternalSurname_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbProfPaternalSurnameCounter.Text = $"{txtProfPaternalSurname.Text.Length}/25";
        }

        private void txtProfMaternalSurname_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbProfMaternalSurnameCounter.Text = $"{txtProfMaternalSurname.Text.Length}/25";
        }

        private void txtProfUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbProfUsernameCounter.Text = $"{txtProfUsername.Text.Length}/25";
        }

        private void txtProfPhone_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbProfPhoneCounter.Text = $"{txtProfPhone.Text.Length}/25";
        }
    }
}
