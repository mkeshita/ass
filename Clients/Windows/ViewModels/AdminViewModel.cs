using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace norsu.ass.Server.ViewModels
{
    class AdminViewModel : ViewModelBase
    {
        private AdminViewModel() { }
        private static AdminViewModel _instance;

        public static AdminViewModel Instance => _instance ?? (_instance = new AdminViewModel());

        private int _ScreenIndex = 0;

        public int ScreenIndex
        {
            get => _ScreenIndex;
            set
            {
                if(value == _ScreenIndex)
                    return;
                _ScreenIndex = value;
                OnPropertyChanged(nameof(ScreenIndex));
            }
        }

        private ICommand _showDevCommand;

        public ICommand ShowDevCommand => _showDevCommand ?? (_showDevCommand = new DelegateCommand(d =>
        {
            ScreenIndex = 4;
            MainViewModel.Instance.Screen = MainViewModel.ADMIN;
        }));

        public void CancelEdit()
        {
            Models.User.CancelLastEdit();
            Models.Office.CancelLastEdit();
        }
    }
}
