using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        
    }
}
