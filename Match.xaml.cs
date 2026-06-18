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

        private ObservableCollection<WordSlot> _wordSlots = new ObservableCollection<WordSlot>();
        private ObservableCollection<ChatMessage> _chatMessages = new ObservableCollection<ChatMessage>();

        public Match(int matchId, int currentUserId, bool isCreator, string username)
        {
            InitializeComponent();
            _matchId = matchId;
            _currentUserId = currentUserId;
            _isCreator = isCreator;

            ResetHangmanImages();

            InstanceContext context = new InstanceContext(this);
            _gameClient = new GameServiceClient(context);

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
                txtDescription.Text = "Conectando con la partida... Esperando jugadores.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error WCF", MessageBoxButton.OK, MessageBoxImage.Error);
                ReturnToMenu();
            }
        }

        public void OnGameStarted(GameContextDTO gameContext)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lbMatchId.Content = gameContext.MatchId;
                lbCategory.Content = gameContext.CategoryName;
                txtDescription.Text = $"Categoría: {gameContext.CategoryName}\nDescripción: {gameContext.WordDescription}";

                _wordSlots.Clear();
                for (int i = 0; i < gameContext.WordLength; i++)
                {
                    _wordSlots.Add(new WordSlot { Index = i, Letter = '_', IsEditing = false });
                }

                if (_isCreator)
                {
                    DisableKeyboard();
                    txtDescription.Text += "\n\nERES EL CREADOR: Espera a que el retador proponga una letra.";
                    bdrCreatorGuide.Visibility = Visibility.Visible;

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
                    txtDescription.Text += "\n\nERES EL ADIVINADOR: Tu turno. Selecciona una letra.";
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
                    txtDescription.Text = $"Propusiste la letra {guessedLetter}. Esperando que el creador la evalúe...";
                }
            });
        }

        public void OnTurnResultReceived(TurnResultDTO turnResult)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (turnResult.IsCorrect)
                {
                    txtDescription.Text = $"¡La letra '{turnResult.GuessedLetter}' ES CORRECTA!";
                    UpdateWordDisplay(turnResult.GuessedLetter, turnResult.CorrectPositions);
                }
                else
                {
                    txtDescription.Text = $"La letra '{turnResult.GuessedLetter}' es INCORRECTA.";
                    DrawHangmanPart(turnResult.CurrentMistakes);
                }

                if (!_isCreator)
                {
                    txtDescription.Text += "\nTu turno. Selecciona otra letra.";
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
                        mensaje = "¡El adivinador ha completado la palabra y GANA la partida!";
                        break;
                    case MatchEndReason.MaxMistakesReached:
                        mensaje = "¡El muñeco ha sido ahorcado! El creador GANA la partida.";
                        break;
                    case MatchEndReason.Abandoned:
                        mensaje = "Un jugador ha abandonado. La partida ha terminado por penalización.";
                        break;
                    case MatchEndReason.Timeout:
                        mensaje = "El tiempo se agotó. Partida finalizada por inactividad.";
                        break;
                }

                MessageBox.Show(mensaje, "Fin del Juego", MessageBoxButton.OK, MessageBoxImage.Information);
                ReturnToMenu();
            });
        }

        public void OnTimerTick(int secondsLeft)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.Title = $"Ahorcado Multijugador - Tiempo restante: {secondsLeft}s";
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
                catch (Exception)
                {
                    MessageBox.Show("No se pudo enviar el mensaje. Verifica tu conexión.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBoxResult result = MessageBox.Show(
                $"El retador propone la letra: '{letter}'.\n\n¿La palabra contiene esta letra?",
                "Evaluar Turno", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _currentEvaluatedLetter = letter;
                txtDescription.Text = $"Haz clic en los guiones donde aparece la letra '{letter}' y presiona Confirmar.";

                foreach (var slot in _wordSlots)
                {
                    if (slot.Letter == '_')
                    {
                        slot.IsEditing = true;
                    }
                }

                btnConfirmPositions.Visibility = Visibility.Visible;
            }
            else
            {
                _gameClient.SubmitTurnResult(_matchId, _currentUserId, false, null);
                txtDescription.Text = "Evaluación enviada (Incorrecto). Esperando siguiente turno...";
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

            imgBackground.Source = new BitmapImage(new Uri($"pack://application:,,,{rutaImagen}"));
        }

        private void ResetHangmanImages()
        {
            imgBackground.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/Images/Game.png"));
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

            _gameClient.SendGuess(_matchId, _currentUserId, letter);
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
            MessageBoxResult dialogResult = MessageBox.Show("¿Seguro que deseas abandonar la partida? Serás penalizado.", "Abandonar Partida", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (dialogResult == MessageBoxResult.Yes)
            {
                _gameClient.LeaveMatch(_matchId, _currentUserId);
                ReturnToMenu();
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
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Tiempo de espera agotado al cerrar la comunicación: {ex.Message}");
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"Error de comunicación al cerrar la ventana: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al cerrar la ventana: {ex.Message}");
            }
        }

        private void ReturnToMenu()
        {
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

        private void btnConfirmPositions_Click(object sender, RoutedEventArgs e)
        {
            int[] positions = _wordSlots
                .Where(s => s.Letter == _currentEvaluatedLetter)
                .Select(s => s.Index)
                .ToArray();

            if (positions.Length == 0)
            {
                MessageBox.Show("Debes seleccionar al menos una posición si indicaste que la letra existe.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var slot in _wordSlots)
            {
                slot.IsEditing = false;
            }
            btnConfirmPositions.Visibility = Visibility.Collapsed;

            _gameClient.SubmitTurnResult(_matchId, _currentUserId, true, positions);
            txtDescription.Text = "Posiciones confirmadas y enviadas. Esperando siguiente turno...";
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