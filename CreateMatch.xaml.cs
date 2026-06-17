using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HangmanClient.CatalogServiceRef;
using HangmanClient.MatchmakingServiceRef;

namespace HangmanClient
{
    public partial class CreateMatch : Window
    {
        private int _matchId = 0;
        private string _languageCode;
        private DispatcherTimer _opponentTimer;
        private bool _isCreator = true;
        private bool _isNavigatingAway = false;

        public CreateMatch() { InitializeComponent(); }

        public CreateMatch(string languageCode)
        {
            InitializeComponent();
            _languageCode = languageCode;
            _isCreator = true;

            InitializeSetup();
        }

        public CreateMatch(string languageCode, int matchId)
        {
            InitializeComponent();
            _languageCode = languageCode;
            _matchId = matchId;
            _isCreator = false;

            InitializeSetup();
        }

        private void InitializeSetup()
        {
            btnStart.IsEnabled = false;

            if (_isCreator)
            {
                lbWaitingOpponent.Visibility = Visibility.Collapsed;
                ExecuteLoadCategoriesAsync();
            }
            else
            {
                cmbCategory.IsEnabled = false;
                cmbWord.IsEnabled = false;
                btnStart.Visibility = Visibility.Collapsed;

                lblMatchIdDisplay.Content = _matchId.ToString("D4");
                lbWaitingOpponent.Content = "ESPERANDO A QUE EL ANFITRIÓN INICIE...";
                lbWaitingOpponent.Visibility = Visibility.Visible;

                StartCheckingForOpponent();
            }
        }

        private void StartCheckingForOpponent()
        {
            _opponentTimer = new DispatcherTimer();
            _opponentTimer.Interval = TimeSpan.FromSeconds(3);
            _opponentTimer.Tick += CheckOpponentStatusAsync;
            _opponentTimer.Start();
        }

        private async void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCategory.SelectedValue == null) return;

            int selectedCategoryId = Convert.ToInt32(cmbCategory.SelectedValue);
            var words = await FetchWordsByCategoryFromServerAsync(selectedCategoryId);

            if (words != null)
            {
                PopulateWordsComboBox(words);
            }
        }

        private async void cmbWord_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCategory.SelectedItem == null || cmbWord.SelectedValue == null) return;

            var selectedCategoryDto = (CategoryDTO)cmbCategory.SelectedItem;
            string categoryName = selectedCategoryDto.CategoryName;
            string wordText = cmbWord.SelectedValue.ToString();

            int generatedId = await RequestCreateMatchOnServerAsync(categoryName, wordText);

            if (generatedId > 0)
            {
                PrepareLobbyForCreator(generatedId);
            }
        }

        private async void CheckOpponentStatusAsync(object sender, EventArgs e)
        {
            _opponentTimer.Stop();

            if (_matchId == 0) return;

            var matchStatus = await FetchMatchStatusFromServerAsync(_matchId);

            if (matchStatus != null)
            {
                ProcessMatchStatusResponse(matchStatus);
            }
            else
            {
                _opponentTimer.Start();
            }
        }

        private async void btnLeave_Click(object sender, RoutedEventArgs e)
        {
            if (_matchId == 0)
            {
                RedirectToMainMenu();
                return;
            }

            btnLeave.IsEnabled = false;
            bool successfullyLeft = await RequestLeaveMatchOnServerAsync();

            if (successfullyLeft)
            {
                RedirectToMainMenu();
            }
            else
            {
                btnLeave.IsEnabled = true;
                _opponentTimer?.Start();
            }
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            bool successfullyStarted = await RequestStartMatchOnServerAsync();

            if (successfullyStarted)
            {
                _opponentTimer?.Stop();
                MessageBox.Show("¡Partida iniciada! Redireccionando al tablero...", "Éxito");
                RedirectToMatch();
            }
            else
            {
                btnStart.IsEnabled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isNavigatingAway) return;

            if (_matchId != 0 && btnLeave.IsEnabled)
            {
                e.Cancel = true;
                btnLeave_Click(this, null);
            }
        }

        private void PopulateCategoriesComboBox(CategoryDTO[] categories)
        {
            cmbCategory.ItemsSource = null;
            cmbCategory.ItemsSource = categories;
            cmbCategory.DisplayMemberPath = "CategoryName";
            cmbCategory.SelectedValuePath = "CategoryId";
        }

        private void PopulateWordsComboBox(WordDTO[] words)
        {
            cmbWord.ItemsSource = words;
            cmbWord.DisplayMemberPath = "WordText";
            cmbWord.SelectedValuePath = "WordText";
        }

        private void PrepareLobbyForCreator(int createdMatchId)
        {
            _matchId = createdMatchId;
            lblMatchIdDisplay.Content = _matchId.ToString("D4");

            lbWaitingOpponent.Content = "ESPERANDO JUGADOR...";
            lbWaitingOpponent.Visibility = Visibility.Visible;

            cmbCategory.IsEnabled = false;
            cmbWord.IsEnabled = false;

            StartCheckingForOpponent();
        }

        private void ProcessMatchStatusResponse(AvailableMatchDTO matchStatus)
        {
            if (_isCreator)
            {
                UpdateCreatorLobbyUI(matchStatus);
            }
            else
            {
                EvaluateChallengerMatchState(matchStatus);
            }
        }

        private void UpdateCreatorLobbyUI(AvailableMatchDTO matchStatus)
        {
            if (!string.IsNullOrEmpty(matchStatus.ChallengerUsername))
            {
                lbWaitingOpponent.Content = $"RIVAL CONECTADO: {matchStatus.ChallengerUsername}";
                btnStart.IsEnabled = true;
            }
            else
            {
                lbWaitingOpponent.Content = "ESPERANDO JUGADOR...";
                btnStart.IsEnabled = false;
            }

            _opponentTimer.Start();
        }

        private void EvaluateChallengerMatchState(AvailableMatchDTO matchStatus)
        {
            if (matchStatus.StatusId == 2)
            {
                MessageBox.Show("¡El anfitrión ha iniciado la partida!", "¡A jugar!");
                RedirectToMatch();
            }
            else if (matchStatus.StatusId == 4)
            {
                MessageBox.Show("El anfitrión ha cancelado la sala.", "Sala Cerrada",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RedirectToMainMenu();
            }
            else
            {
                _opponentTimer.Start();
            }
        }

        private async void ExecuteLoadCategoriesAsync()
        {
            try
            {
                using (var client = new CatalogServiceClient())
                {
                    var categories = await client.GetCategoriesAsync(_languageCode);
                    PopulateCategoriesComboBox(categories);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar categorías desde el catálogo: {ex.Message}",
                                "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<WordDTO[]> FetchWordsByCategoryFromServerAsync(int categoryId)
        {
            try
            {
                using (var client = new CatalogServiceClient())
                {
                    return await client.GetWordsByCategoryAsync(categoryId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al recuperar las palabras de la categoría: {ex.Message}",
                                "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async Task<int> RequestCreateMatchOnServerAsync(string categoryName, string wordText)
        {
            try
            {
                using (var client = new MatchmakingServiceClient())
                {
                    string currentUsername = UserSession.Instance.CurrentUser.Username;
                    return await client.CreateMatchAsync(currentUsername, categoryName, wordText, _languageCode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo registrar la sala en la base de datos: {ex.Message}",
                                "Error de Redirección", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
        }

        private async Task<AvailableMatchDTO> FetchMatchStatusFromServerAsync(int matchId)
        {
            try
            {
                using (var client = new MatchmakingServiceClient())
                {
                    return await client.GetMatchStatusAsync(matchId);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<bool> RequestLeaveMatchOnServerAsync()
        {
            try
            {
                using (var client = new MatchmakingServiceClient())
                {
                    _opponentTimer?.Stop();
                    return await client.LeaveMatchAsync(_matchId, _isCreator);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fallo de conexión al intentar abandonar la sala: {ex.Message}",
                                "Error de Red", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async Task<bool> RequestStartMatchOnServerAsync()
        {
            try
            {
                using (var client = new MatchmakingServiceClient())
                {
                    return await client.StartMatchAsync(_matchId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar la partida: {ex.Message}",
                                "Error de Red", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void RedirectToMainMenu()
        {
            _isNavigatingAway = true;
            MainMenu mainMenuWindow = new MainMenu();
            mainMenuWindow.Show();
            this.Close();
        }

        private void RedirectToMatch()
        {
            _isNavigatingAway = true;

            int currentUserId = UserSession.Instance.CurrentUser.UserId;
            string currentUsername = UserSession.Instance.CurrentUser.Username;

            Match gameBoard = new Match(_matchId, currentUserId, _isCreator, currentUsername);
            gameBoard.Show();
            this.Close();
        }
    }
}