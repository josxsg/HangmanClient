using System;
using HangmanClient.AccountServiceRef; 

namespace HangmanClient
{
    public class UserSession
    {
        private static readonly Lazy<UserSession> _instance =
            new Lazy<UserSession>(() => new UserSession());

        public static UserSession Instance => _instance.Value;

        private UserSession()
        {
        }

        public UserDTO CurrentUser { get; set; }

        public bool IsLoggedIn => CurrentUser != null;

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}