using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using norsu.ass.Models;
using norsu.ass.Network;

namespace norsu.ass.Server.ViewModels
{
    class LoginViewModel : ViewModelBase
    {
        private LoginViewModel() { }

        private string _Username;

        public string Username
        {
            get => _Username;
            set
            {
                if(value == _Username)
                    return;
                _Username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        private string _Password;

        public string Password
        {
            get => _Password;
            set
            {
                if(value == _Password)
                    return;
                _Password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        private bool _IsProcessing;

        public bool IsProcessing
        {
            get => _IsProcessing;
            set
            {
                if(value == _IsProcessing)
                    return;
                _IsProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
            }
        }

        private string _ErrorMessage;

        public string ErrorMessage
        {
            get => _ErrorMessage;
            set
            {
                if(value == _ErrorMessage)
                    return;
                _ErrorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        private bool _HasError;

        public bool HasError
        {
            get => _HasError;
            set
            {
                if(value == _HasError)
                    return;
                _HasError = value;
                OnPropertyChanged(nameof(HasError));
            }
        }

        private User _User;

        public User User
        {
            get {

                return _User;
                
            }
            set
            {
                _User = value;
                OnPropertyChanged(nameof(User));
            }
        }

        private int _SessionId;

        public int SessionId
        {
            get => _SessionId;
            set
            {
                if(value == _SessionId)
                    return;
                _SessionId = value;
                OnPropertyChanged(nameof(SessionId));
            }
        }

        private bool _LoginSuccess;

        public bool LoginSuccess
        {
            get => _LoginSuccess;
            set
            {
                if(value == _LoginSuccess)
                    return;
                _LoginSuccess = value;
                OnPropertyChanged(nameof(LoginSuccess));
            }
        }

        private ICommand _logoutCommand;

        public ICommand LogoutCommand => _logoutCommand ?? (_logoutCommand = new DelegateCommand(d =>
        {
            User = null;
            Messenger.Default.Broadcast(Messages.Logout);
        }));
        
        private ICommand _loginCommand;

        public ICommand LoginCommand => _loginCommand ?? (_loginCommand = new DelegateCommand(
        async d =>
        {
            HasError = false;
            LoginSuccess = false;
            ErrorMessage = "Signing in...";
            IsProcessing = true;
            var result = await Client.Login(Username, Password);
            
            if (result == null)
            {
                ErrorMessage = "Server is offline.";
                HasError = true;
                LoginSuccess = false;
                await TaskEx.Delay(1000);
                IsProcessing = false;
                return;
            }

            if(!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                LoginSuccess = false;
                HasError = true;
                await TaskEx.Delay(1000);
                IsProcessing = false;
                return;
            }

            var user = User.Cache.FirstOrDefault(x => x.Id == result.User?.Id);
            
            LoginSuccess = true;
            ErrorMessage = "AUTHENTICATION SUCCESSFULL";
            
            User = user;

            await TaskEx.Delay(1000);
            IsProcessing = false;
            Username = "";
            Password = "";
            Messenger.Default.Broadcast(Messages.LoggedIn);

        }, d => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrEmpty(Password)));

        private static LoginViewModel _instance;
        public static LoginViewModel Instance => _instance ?? (_instance = new LoginViewModel());
    
    }
}
