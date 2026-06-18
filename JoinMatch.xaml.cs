using System;
using System.Collections.Generic;
using System.Linq;
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

            ExecuteLoadAvailableMatchesAsync();
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
                MessageBox.Show("¡Te has unido a la sala con éxito! Entrando a la sala de espera...",
                                "Partida Inicializada", MessageBoxButton.OK, MessageBoxImage.Information);
                RedirectToLobby(selectedMatchId);
            }
            else
            {
                MessageBox.Show("No fue posible ingresar a la sala. Es probable que otro jugador se haya unido primero.",
                                "Sala No Disponible", MessageBoxButton.OK, MessageBoxImage.Warning);

                clickedButton.IsEnabled = true;
                ExecuteLoadAvailableMatchesAsync();
            }
        }

        private async void ExecuteLoadAvailableMatchesAsync()
        {
            try
            {
                using (var client = new MatchmakingServiceClient())
                {
                    AvailableMatchDTO[] availableMatches = await client.GetAvailableMatchesAsync(_languageCode);
                    PopulateAvailableMatchesGrid(availableMatches);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar con el servidor para listar partidas: {ex.Message}",
                                "Error de Red", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async Task<bool> TryJoinMatchSessionAsync(int matchId)
        {
            try
            {
                using (var client = new MatchmakingServiceClient())
                {
                    string currentUsername = UserSession.Instance.CurrentUser.Username;
                    return await client.JoinMatchAsync(matchId, currentUsername);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un fallo de red al intentar conectar con el servidor: {ex.Message}",
                                "Error de Comunicación", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}