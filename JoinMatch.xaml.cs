using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HangmanClient.MatchmakingServiceRef;

namespace HangmanClient
{
    public partial class JoinMatch : Window
    {
        private string _languageCode;

        public JoinMatch()
        {
            InitializeComponent();
        }

        public JoinMatch(string languageCode)
        {
            InitializeComponent();
            _languageCode = languageCode;

            _ = ExecuteLoadAvailableMatchesAsync();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            RedirectToMainMenu();
        }

        private async void btnJoin_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton == null || clickedButton.Tag == null) return;

            int selectedMatchId = Convert.ToInt32(clickedButton.Tag);
            clickedButton.IsEnabled = false;

            bool isJoinSuccessful = await TryJoinMatchSessionAsync(selectedMatchId);

            if (isJoinSuccessful)
            {
                MessageBox.Show(Properties.Resources.mbJoinSuccess,
                                Properties.Resources.mbMatchStarted, MessageBoxButton.OK, MessageBoxImage.Information);
                RedirectToLobby(selectedMatchId);
            }
            else
            {
                MessageBox.Show(Properties.Resources.mbJoinError,
                                Properties.Resources.mbLobbyNotAv, MessageBoxButton.OK, MessageBoxImage.Warning);

                clickedButton.IsEnabled = true;
                _ = ExecuteLoadAvailableMatchesAsync();
            }
        }

        private async Task ExecuteLoadAvailableMatchesAsync()
        {
            try
            {
                using (var client = new MatchmakingServiceClient())
                {
                    AvailableMatchDTO[] availableMatches = await client.GetAvailableMatchesAsync(_languageCode);
                    PopulateAvailableMatchesGrid(availableMatches);
                }
            }
            catch (EndpointNotFoundException)
            {
                MessageBox.Show(Properties.Resources.mbServerUnavailable, Properties.Resources.mbNetworkError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Properties.Resources.mbServerTimeout, Properties.Resources.mbNetworkError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (CommunicationException)
            {
                MessageBox.Show(Properties.Resources.mbServerUnavailable, Properties.Resources.mbNetworkError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception)
            {
                MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateAvailableMatchesGrid(AvailableMatchDTO[] availableMatches)
        {
            listaPartidas.ItemsSource = availableMatches.Select(match => new
            {
                MatchId = match.MatchId,
                CreationDate = match.CreationDate.ToString("dd/MM/yyyy HH:mm"),
                CreatorUsername = match.CreatorUsername,
                CategoryName = match.CategoryName
            }).ToList();
        }

        private void RedirectToMainMenu()
        {
            MainMenu mainMenuWindow = new MainMenu();
            mainMenuWindow.Show();
            this.Close();
        }

        private void RedirectToLobby(int matchId)
        {
            CreateMatch lobbyWindow = new CreateMatch(_languageCode, matchId);
            lobbyWindow.Show();
            this.Close();
        }

        private static async Task<bool> TryJoinMatchSessionAsync(int matchId)
        {
            try
            {
                using (var client = new MatchmakingServiceClient())
                {
                    string currentUsername = UserSession.Instance.CurrentUser.Username;
                    return await client.JoinMatchAsync(matchId, currentUsername);
                }
            }
            catch (EndpointNotFoundException)
            {
                MessageBox.Show(Properties.Resources.mbServerUnavailable, Properties.Resources.mbNetworkError, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Properties.Resources.mbServerTimeout, Properties.Resources.mbNetworkError, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (CommunicationException)
            {
                MessageBox.Show(Properties.Resources.mbServerUnavailable, Properties.Resources.mbNetworkError, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception)
            {
                MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}