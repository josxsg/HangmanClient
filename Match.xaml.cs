using System;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using HangmanClient.GameServiceRef; 

namespace HangmanClient
{
    public partial class Match : Window, IGameServiceCallback
    {
        private GameServiceClient _gameClient;
        private int _matchId;
        private int _currentUserId;
        private bool _isCreator;
        private string _actualWord; 
        private string _username;
        private bool _isNavigatingAway = false;

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

                _actualWord = string.Join(" ", Enumerable.Repeat("_", gameContext.WordLength));
                txtPalabraOculta.Text = _actualWord;

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

        private void EvaluateGuessAsCreator(char letter)
        {
            MessageBoxResult result = MessageBox.Show(
                $"El retador propone la letra: '{letter}'.\n\n¿La palabra contiene esta letra?",
                "Evaluar Turno", MessageBoxButton.YesNo, MessageBoxImage.Question);

            bool isCorrect = (result == MessageBoxResult.Yes);
            int[] positions = null;

            if (isCorrect)
            {
                string input = PromptForPositions(letter);

                if (!string.IsNullOrWhiteSpace(input))
                {
                    try
                    {
                        positions = input.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
                    }
                    catch
                    {
                        MessageBox.Show("Formato inválido. Se asumirá error del creador.", "Error");
                        isCorrect = false;
                    }
                }
                else
                {
                    isCorrect = false; 
                }
            }

            _gameClient.SubmitTurnResult(_matchId, _currentUserId, isCorrect, positions);
            txtDescription.Text = "Evaluación enviada. Esperando siguiente turno del retador...";
        }

        private void UpdateWordDisplay(char letter, int[] positions)
        {
            string[] chars = _actualWord.Split(' ');
            foreach (int pos in positions)
            {
                if (pos >= 0 && pos < chars.Length)
                {
                    chars[pos] = letter.ToString();
                }
            }
            _actualWord = string.Join(" ", chars);
            txtPalabraOculta.Text = _actualWord;
        }

        private void DrawHangmanPart(int mistakes)
        {
            if (mistakes >= 1)
            {
                imgCabeza.Visibility = Visibility.Visible;
            }

            if (mistakes >= 2)
            {
                imgCuerpo.Visibility = Visibility.Visible;
            }

            if (mistakes >= 3)
            {
                imgBrazoIzq.Visibility = Visibility.Visible;
            }

            if (mistakes >= 4)
            {
                imgBrazoDer.Visibility = Visibility.Visible;
            }

            if (mistakes >= 5)
            {
                imgPiernaIzq.Visibility = Visibility.Visible;
            }

            if (mistakes >= 6)
            {
                imgPiernaDer.Visibility = Visibility.Visible;
            }
        }

        private void ResetHangmanImages()
        {
            imgCabeza.Visibility = Visibility.Hidden;
            imgCuerpo.Visibility = Visibility.Hidden;
            imgBrazoIzq.Visibility = Visibility.Hidden;
            imgBrazoDer.Visibility = Visibility.Hidden;
            imgPiernaIzq.Visibility = Visibility.Hidden;
            imgPiernaDer.Visibility = Visibility.Hidden;
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

        private string PromptForPositions(char letter)
        {
            Window prompt = new Window()
            {
                Title = "Posiciones Correctas",
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            StackPanel stack = new StackPanel() { Margin = new Thickness(10) };
            stack.Children.Add(new TextBlock() { Text = $"¿En qué posiciones está la '{letter}'?\n(Iniciando en 0. Escribe separando con comas ej: 0, 2, 4)", Margin = new Thickness(0, 0, 0, 10) });
            TextBox txtInput = new TextBox() { Margin = new Thickness(0, 0, 0, 10) };
            Button btnOk = new Button() { Content = "Aceptar", Width = 80, IsDefault = true };
            btnOk.Click += (s, e) => { prompt.DialogResult = true; };
            stack.Children.Add(txtInput);
            stack.Children.Add(btnOk);
            prompt.Content = stack;

            if (prompt.ShowDialog() == true)
            {
                return txtInput.Text;
            }
            return string.Empty;
        }

        public class LetterPosition
        {
            public char Letter { get; set; }
            public int Position { get; set; }
        }
    }
}