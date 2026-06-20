using System;
using System.Linq;
using System.ServiceModel; 
using System.Windows;
using System.Windows.Controls;
using HangmanClient.GameServiceRef;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Threading;

namespace HangmanClient
{
    public partial class Match : Window, IGameServiceCallback
    {
        private GameServiceClient _gameClient;
        private int _matchId;
        private int _currentUserId;
        private bool _isCreator;
        private string _username;
        private bool _isNavigatingAway = false;
        private char _currentEvaluatedLetter;
        private string _secretWord;
        private DispatcherTimer _watchdogTimer;

        private ObservableCollection<WordSlot> _wordSlots = new ObservableCollection<WordSlot>();
        private ObservableCollection<ChatMessage> _chatMessages = new ObservableCollection<ChatMessage>();

        private const string GAME_BACKGROUND_IMAGE_PATH = "/Properties/Images/Game.png";

        public Match(int matchId, int currentUserId, bool isCreator, string username)
        {
            InitializeComponent();
            _matchId = matchId;
            _currentUserId = currentUserId;
            _isCreator = isCreator;

            ResetHangmanImages();

            InstanceContext context = new InstanceContext(this);
            _gameClient = new GameServiceClient(context);

            _gameClient.InnerChannel.Faulted += GameClient_ConnectionLost;
            _gameClient.InnerChannel.Closed += GameClient_ConnectionLost;

            _watchdogTimer = new DispatcherTimer();
            _watchdogTimer.Interval = TimeSpan.FromSeconds(5); 
            _watchdogTimer.Tick += (s, ev) =>
            {
                _watchdogTimer.Stop();
                HandleConnectionError(); 
            };

            this.Loaded += Match_Loaded;
            this.Closing += Match_Closing;
            _username = username;

            itemsChat.ItemsSource = _chatMessages;
            icPalabraOculta.ItemsSource = _wordSlots;

            btnEnviarMensaje.Click += btnEnviarMensaje_Click;
            txtMensajeChat.KeyDown += txtMensajeChat_KeyDown;
        }

        private void Match_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _gameClient.JoinGameChannel(_matchId, _currentUserId);
                txtDescription.Text = Properties.Resources.lbJoining;
                _watchdogTimer.Start();
            }
            catch (EndpointNotFoundException)
            {
                HandleConnectionError();
            }
            catch (TimeoutException)
            {
                HandleConnectionError();
            }
            catch (CommunicationException)
            {
                HandleConnectionError();
            }
            catch (Exception)
            {
                MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
                ReturnToMenu();
            }
        }

        public void OnGameStarted(GameContextDTO gameContext)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lbMatchId.Content = gameContext.MatchId;
                lbCategory.Content = gameContext.CategoryName;
                txtDescription.Text = string.Format(Properties.Resources.txtCategoryDescription, gameContext.CategoryName, "\n" + gameContext.WordDescription);
                _wordSlots.Clear();
                for (int i = 0; i < gameContext.WordLength; i++)
                {
                    _wordSlots.Add(new WordSlot { Index = i, Letter = '_', IsEditing = false });
                }

                if (_isCreator)
                {
                    _secretWord = gameContext.SecretWord;
                    DisableKeyboard();
                    txtDescription.Text += "\n\n" + Properties.Resources.txtIsCreator; bdrCreatorGuide.Visibility = Visibility.Visible;

                    if (!string.IsNullOrEmpty(gameContext.SecretWord))
                    {
                        var guideList = gameContext.SecretWord
                            .Select((c, index) => new LetterPosition { Letter = c, Position = index })
                            .ToList();

                        icSecretWordGuide.ItemsSource = guideList;
                    }
                }
                else
                {
                    txtDescription.Text += "\n\n" + Properties.Resources.txtGuesserTurn; bdrCreatorGuide.Visibility = Visibility.Collapsed;
                }
            });
        }

        public void OnGuessReceived(char guessedLetter)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_isCreator)
                {
                    EvaluateGuessAsCreator(guessedLetter);
                }
                else
                {
                    txtDescription.Text = string.Format(Properties.Resources.txtWaitingEv, guessedLetter);
                }
            });
        }

        public void OnTurnResultReceived(TurnResultDTO turnResult)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (turnResult.IsCorrect)
                {
                    txtDescription.Text = string.Format(Properties.Resources.txtLetterCorrect, turnResult.GuessedLetter);
                    UpdateWordDisplay(turnResult.GuessedLetter, turnResult.CorrectPositions);
                }
                else
                {
                    txtDescription.Text = string.Format(Properties.Resources.txtLetterIncorrect, turnResult.GuessedLetter);
                    DrawHangmanPart(turnResult.CurrentMistakes);
                }

                if (!_isCreator)
                {
                    txtDescription.Text += "\n" + Properties.Resources.txtYourTurnGuess;
                }
            });
        }

        public void OnGameEnded(GameEndDTO endResult)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_isNavigatingAway)
                {
                    return;
                }
                string mensaje = "";
                switch (endResult.Reason)
                {
                    case MatchEndReason.WordGuessed:
                        mensaje = Properties.Resources.msgEndWordGuessed;
                        break;
                    case MatchEndReason.MaxMistakesReached:
                        mensaje = Properties.Resources.msgEndMaxMistakes;
                        break;
                    case MatchEndReason.Abandoned:
                        mensaje = Properties.Resources.msgEndAbandoned;
                        break;
                    case MatchEndReason.Timeout:
                        mensaje = Properties.Resources.msgEndTimeout;
                        break;
                }

                MessageBox.Show(mensaje, Properties.Resources.msgTitleGameOver, MessageBoxButton.OK, MessageBoxImage.Information);
                ReturnToMenu();
            });
        }

        public void OnTimerTick(int secondsLeft)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.Title = string.Format(Properties.Resources.msgTimeRemaining, secondsLeft);
                _watchdogTimer.Stop();
                _watchdogTimer.Start();
            });
        }

        public void OnEvaluationError()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var slot in _wordSlots)
                {
                    slot.IsEditing = false;
                    if (slot.Letter == _currentEvaluatedLetter)
                    {
                        slot.Letter = '_';
                    }
                }
                btnConfirmPositions.Visibility = Visibility.Collapsed;
                MessageBox.Show(Properties.Resources.msgEvaluationError, Properties.Resources.msgTitleEvaluationError, MessageBoxButton.OK, MessageBoxImage.Warning);
                EvaluateGuessAsCreator(_currentEvaluatedLetter);
            });
        }

        private void btnEnviarMensaje_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void txtMensajeChat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        private void SendMessage()
        {
            string message = txtMensajeChat.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                try
                {
                    _gameClient.SendChatMessage(_matchId, _username, message);
                    txtMensajeChat.Clear();
                    txtMensajeChat.Focus();
                }
                catch (EndpointNotFoundException)
                {
                    HandleConnectionError();
                }
                catch (TimeoutException)
                {
                    HandleConnectionError();
                }
                catch (CommunicationException)
                {
                    HandleConnectionError();
                }
                catch (Exception)
                {
                    MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void OnChatMessageReceived(string senderUsername, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string color = (senderUsername == _username) ? "#005A9C" : "#4A4A4A";

                _chatMessages.Add(new ChatMessage
                {
                    PlayerName = senderUsername + ":",
                    MessageText = message,
                    NameColor = color
                });

                scrollChat.ScrollToEnd();
            });
        }


        private void EvaluateGuessAsCreator(char letter)
        {
            _currentEvaluatedLetter = letter;

            string questionMessage = string.Format(Properties.Resources.msgChallengerProposes, letter) + "\n\n" + Properties.Resources.msgContainsLetterQuestion;
            MessageBoxResult result = MessageBox.Show(questionMessage, Properties.Resources.msgTitleEvaluateTurn, MessageBoxButton.YesNo, MessageBoxImage.Question);

            string secretUpper = _secretWord.ToUpper();
            char letterUpper = char.ToUpper(letter);
            bool letterExists = secretUpper.Contains(letterUpper);

            if (result == MessageBoxResult.Yes)
            {
                if (!letterExists)
                {
                    MessageBox.Show(Properties.Resources.msgEvaluationError, Properties.Resources.msgTitleEvaluationError, MessageBoxButton.OK, MessageBoxImage.Warning);
                    EvaluateGuessAsCreator(letter);
                    return;
                }

                EnableWordSlotsForEditing(letter);
            }
            else
            {
                SubmitIncorrectTurnResult();
            }
        }

        private void EnableWordSlotsForEditing(char letter)
        {
            txtDescription.Text = string.Format(Properties.Resources.msgClickDashes, letter);

            foreach (var slot in _wordSlots)
            {
                if (slot.Letter == '_')
                {
                    slot.IsEditing = true;
                }
            }

            btnConfirmPositions.Visibility = Visibility.Visible;
        }

        private void SubmitIncorrectTurnResult()
        {
            try
            {
                _gameClient.SubmitTurnResult(_matchId, _currentUserId, false, null);
                txtDescription.Text = Properties.Resources.msgEvaluationSent;
            }
            catch (EndpointNotFoundException)
            {
                HandleConnectionError();
            }
            catch (TimeoutException)
            {
                HandleConnectionError();
            }
            catch (CommunicationException)
            {
                HandleConnectionError();
            }
            catch (Exception)
            {
                MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateWordDisplay(char letter, int[] positions)
        {
            if (positions == null) return;

            foreach (int pos in positions)
            {
                if (pos >= 0 && pos < _wordSlots.Count)
                {
                    _wordSlots[pos].Letter = letter;
                }
            }
        }

        private void DrawHangmanPart(int mistakes)
        {
            string rutaImagen = "/Properties/Images/Game.png";

            switch (mistakes)
            {
                case 1:
                    rutaImagen = "/Properties/Images/head.png";
                    break;
                case 2:
                    rutaImagen = "/Properties/Images/body.png";
                    break;
                case 3:
                    rutaImagen = "/Properties/Images/leftArm.png";
                    break;
                case 4:
                    rutaImagen = "/Properties/Images/rightArm.png";
                    break;
                case 5:
                    rutaImagen = "/Properties/Images/leftLeg.png";
                    break;
                case 6:
                    rutaImagen = "/Properties/Images/rightArm.png";
                    break;
            }

            imgBackground.Source = new BitmapImage(new Uri(rutaImagen, UriKind.Relative));
        }

        private void ResetHangmanImages()
        {
            imgBackground.Source = new BitmapImage(new Uri(GAME_BACKGROUND_IMAGE_PATH, UriKind.Relative));
        }

        private void DisableKeyboard()
        {
            btnQ.IsEnabled = false; btnW.IsEnabled = false; btnE.IsEnabled = false; btnR.IsEnabled = false;
            btnT.IsEnabled = false; btnY.IsEnabled = false; btnU.IsEnabled = false; btnI.IsEnabled = false;
            btnO.IsEnabled = false; btnP.IsEnabled = false; btnA.IsEnabled = false; btnS.IsEnabled = false;
            btnD.IsEnabled = false; btnF.IsEnabled = false; btnG.IsEnabled = false; btnH.IsEnabled = false;
            btnJ.IsEnabled = false; btnK.IsEnabled = false; btnL.IsEnabled = false; btnZ.IsEnabled = false;
            btnX.IsEnabled = false; btnC.IsEnabled = false; btnV.IsEnabled = false; btnB.IsEnabled = false;
            btnN.IsEnabled = false; btnM.IsEnabled = false;
        }

        private void HandleKeyPress(Button btn)
        {
            if (_isCreator)
            {
                return;
            }

            char letter = btn.Content.ToString()[0];
            btn.IsEnabled = false;

            try
            {
                _gameClient.SendGuess(_matchId, _currentUserId, letter);
            }
            catch (EndpointNotFoundException)
            {
                HandleConnectionError();
            }
            catch (TimeoutException)
            {
                HandleConnectionError();
            }
            catch (CommunicationException)
            {
                HandleConnectionError();
            }
            catch (Exception)
            {
                MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
                btn.IsEnabled = true;
            }
        }

        private void btnQ_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnQ); }
        private void btnW_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnW); }
        private void btnE_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnE); }
        private void btnR_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnR); }
        private void btnT_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnT); }
        private void btnY_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnY); }
        private void btnU_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnU); }
        private void btnI_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnI); }
        private void btnO_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnO); }
        private void btnP_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnP); }
        private void btnA_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnA); }
        private void btnS_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnS); }
        private void btnD_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnD); }
        private void btnF_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnF); }
        private void btnG_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnG); }
        private void btnH_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnH); }
        private void btnJ_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnJ); }
        private void btnK_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnK); }
        private void btnL_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnL); }
        private void btnZ_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnZ); }
        private void btnX_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnX); }
        private void btnC_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnC); }
        private void btnV_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnV); }
        private void btnB_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnB); }
        private void btnN_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnN); }
        private void btnM_Click(object sender, RoutedEventArgs e) { HandleKeyPress(btnM); }

        private void btnLeave_Click(object sender, RoutedEventArgs e)
        {
            if (_isNavigatingAway)
            {
                return;
            }
            MessageBoxResult dialogResult = MessageBox.Show(Properties.Resources.msgConfirmLeave, Properties.Resources.msgTitleLeaveMatch, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (dialogResult == MessageBoxResult.Yes)
            {
                try
                {
                    _gameClient.LeaveMatch(_matchId, _currentUserId);
                }
                catch (Exception)
                {
                    MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ReturnToMenu();
                }
            }
        }

        private void Match_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_isNavigatingAway)
                {
                    return;
                }
                if (_gameClient.State == CommunicationState.Opened)
                {
                    _gameClient.LeaveMatch(_matchId, _currentUserId);
                    _gameClient.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReturnToMenu()
        {
            if (_watchdogTimer != null)
            {
                _watchdogTimer.Stop();
            }
            if (_isNavigatingAway)
            {
                return;
            }
            _isNavigatingAway = true;
            MainMenu mainMenuWindow = new MainMenu();
            mainMenuWindow.Show();
            this.Close();
        }

        private void btnChat_Click(object sender, RoutedEventArgs e)
        {
            popChat.IsOpen = true;
        }

        private void WordSlot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is WordSlot slot)
            {
                if (slot.Letter == '_')
                {
                    slot.Letter = _currentEvaluatedLetter;
                }
                else if (slot.Letter == _currentEvaluatedLetter)
                {
                    slot.Letter = '_';
                }
            }
        }

        private void GameClient_ConnectionLost(object sender, EventArgs e)
        {
            HandleConnectionError();
        }

        private void HandleConnectionError()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_isNavigatingAway)
                {
                    return;
                }
                _isNavigatingAway = true;

                MessageBox.Show(Properties.Resources.msgConnectionLost, Properties.Resources.msgTitleNetworkError, MessageBoxButton.OK, MessageBoxImage.Error);

                Login loginWindow = new Login();
                loginWindow.Show();
                this.Close();
            });
        }

        private void btnConfirmPositions_Click(object sender, RoutedEventArgs e)
        {
            int[] positions = _wordSlots
                .Where(s => s.Letter == _currentEvaluatedLetter)
                .Select(s => s.Index)
                .ToArray();

            if (positions.Length == 0)
            {
                MessageBox.Show(Properties.Resources.msgSelectAtLeastOne, Properties.Resources.msgTitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var slot in _wordSlots)
            {
                slot.IsEditing = false;
            }
            btnConfirmPositions.Visibility = Visibility.Collapsed;

            try
            {
                _gameClient.SubmitTurnResult(_matchId, _currentUserId, true, positions);
                txtDescription.Text = Properties.Resources.msgPositionsConfirmed;
            }
            catch (EndpointNotFoundException)
            {
                HandleConnectionError();
            }
            catch (TimeoutException)
            {
                HandleConnectionError();
            }
            catch (CommunicationException)
            {
                HandleConnectionError();
            }
            catch (Exception)
            {
                MessageBox.Show(Properties.Resources.mbServerError, Properties.Resources.mbError, MessageBoxButton.OK, MessageBoxImage.Error);
                btnConfirmPositions.Visibility = Visibility.Visible;
            }
        }

        public class LetterPosition
        {
            public char Letter { get; set; }
            public int Position { get; set; }
        }
    }

    public class ChatMessage
    {
        public string PlayerName { get; set; }
        public string MessageText { get; set; }
        public string NameColor { get; set; }
    }

    public class WordSlot : INotifyPropertyChanged
    {
        private char _letter;
        private bool _isEditing;

        public int Index { get; set; }

        public char Letter
        {
            get
            {
                return _letter;
            }
            set
            {
                _letter = value; OnPropertyChanged(nameof(Letter));
            }
        }

        public bool IsEditing
        {
            get
            {
                return _isEditing;
            }
            set
            {
                _isEditing = value; OnPropertyChanged(nameof(IsEditing));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}