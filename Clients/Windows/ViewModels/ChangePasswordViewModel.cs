using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using norsu.ass.Network;

namespace norsu.ass.Server.ViewModels
{
    class ChangePasswordViewModel : ViewModelBase
    {
        private string _CurrentPassword;

        public string CurrentPassword
        {
            get => _CurrentPassword;
            set
            {
                if(value == _CurrentPassword)
                    return;
                _CurrentPassword = value;
                OnPropertyChanged(nameof(CurrentPassword));
            }
        }

        private string _NewPassword;

        public string NewPassword
        {
            get => _NewPassword;
            set
            {
                if(value == _NewPassword)
                    return;
                _NewPassword = value;
                OnPropertyChanged(nameof(NewPassword));
            }
        }

        private string _NewPassword2;

        public string NewPassword2
        {
            get => _NewPassword2;
            set
            {
                if(value == _NewPassword2)
                    return;
                _NewPassword2 = value;
                OnPropertyChanged(nameof(NewPassword2));
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

        private bool _Success;

        public bool Success
        {
            get => _Success;
            set
            {
                if(value == _Success)
                    return;
                _Success = value;
                OnPropertyChanged(nameof(Success));
            }
        }
        
        public async Task<bool> Process()
        {
            Success = false;
            HasError = false;
            IsProcessing = true;
            ErrorMessage = "Processing Request...";
            if (NewPassword != NewPassword2)
            {
                ErrorMessage = "PASSWORD DID NOT MATCH";
                HasError = true;
                await TaskEx.Delay(1111);
                HasError = false;
                IsProcessing = false;
                return false;
            }
            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword))
            {
                ErrorMessage = "INVALID PASSWORD";
                HasError = true;
                IsProcessing = false;
                await TaskEx.Delay(1111);
                HasError = false;
                IsProcessing = false;
                return false;
            }
            
            var res = await Client.ChangePassword(CurrentPassword, NewPassword, LoginViewModel.Instance.User?.Id ?? 0);
            
            if (res?.Success??false)
            {
                ErrorMessage = "Password Changed";
                HasError = false;
                Success = true;
                await TaskEx.Delay(1111);
                IsProcessing = false;
                return true;
            }
            
            ErrorMessage = res?.ErrorMessage ?? "REQUEST TIMEOUT";
            Success = false;
            HasError = true;
            await TaskEx.Delay(1111);
            IsProcessing = false;
            return false;

        }
    }
}
