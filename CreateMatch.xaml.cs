using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
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
                _ = ExecuteLoadCategoriesAsync();
            }
            else
            {
                cmbCategory.IsEnabled = false;
                cmbWord.IsEnabled = false;
                btnStart.Visibility = Visibility.Collapsed;

                lblMatchIdDisplay.Content = _matchId.ToString("D4");
                lbWaitingOpponent.Content = Properties.Resources.lbWaitingHost;
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
                MessageBox.Show(Properties.Resources.mbMatchStarted, Properties.Resources.mbSuccess);
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

            lbWaitingOpponent.Content = Properties.Resources.lbWaitingPlayer;
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
                lbWaitingOpponent.Content = string.Format(Properties.Resources.lbOpponentConnected, matchStatus.ChallengerUsername);
                btnStart.IsEnabled = true;
            }
            else
            {
                lbWaitingOpponent.Content = Properties.Resources.lbWaitingPlayer;
                btnStart.IsEnabled = false;
            }

            _opponentTimer.Start();
        }

        private void EvaluateChallengerMatchState(AvailableMatchDTO matchStatus)
        {
            if (matchStatus.StatusId == 2)
            {
                MessageBox.Show(Properties.Resources.mbHostMatchStarted, Properties.Resources.mbToPlay);
                RedirectToMatch();
            }
            else if (matchStatus.StatusId == 4)
            {
                MessageBox.Show(Properties.Resources.mbHostLobbyCancelled, "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RedirectToMainMenu();
            }
            else
            {
                _opponentTimer.Start();
            }
        }

        private async Task ExecuteLoadCategoriesAsync()
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
                HandleWcfException(ex);
            }
        }

        private static async Task<WordDTO[]> FetchWordsByCategoryFromServerAsync(int categoryId)
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
                HandleWcfException(ex);
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
                HandleWcfException(ex);
                return 0;
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
                HandleWcfException(ex);
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
                HandleWcfException(ex);
                return false;
            }
        }

        private static async Task<AvailableMatchDTO> FetchMatchStatusFromServerAsync(int matchId)
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

        private static void HandleWcfException(Exception ex)
        {
            if (ex is TimeoutException)
            {
                MessageBox.Show(Properties.Resources.mbServerTimeout, Properties.Resources.mbNetworkError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (ex is CommunicationException) 
            {
                MessageBox.Show(Properties.Resources.mbServerUnavailable, Properties.Resources.mbNetworkError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
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