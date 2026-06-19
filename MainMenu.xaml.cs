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
using System.Globalization;
using System.Threading;
using HangmanClient.ScoreServiceRef;

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
                                resText = $"GANADA: +{h.Points} pts";
                                resColor = "#2E7D32"; 
                                break;
                            case "Perdida":
                                resText = $"PERDIDA: {h.Points} pts";
                                resColor = "#C62828"; 
                                break;
                            case "Abandonada":
                                resText = $"PERDIDA POR ABANDONO: {h.Points} pts";
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
                            RivalText = $"Rival: {h.RivalUsername}",
                            WordText = $"Palabra: {h.WordText}",
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
                MessageBox.Show($"Error al cargar el historial: {ex.Message}", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que deseas cerrar sesión?", "Cerrar Sesión", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                UserSession.Instance.Logout();

                Login loginWindow = new Login();
                loginWindow.Show();

                this.Close();
            }
        }
    }
}
