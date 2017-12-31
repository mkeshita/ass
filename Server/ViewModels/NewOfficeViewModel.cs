using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace norsu.ass.Server.ViewModels
{
    class NewOfficeViewModel : ViewModelBase
    {
        private string _ShortName;

        public string ShortName
        {
            get => _ShortName;
            set
            {
                if(value == _ShortName)
                    return;
                _ShortName = value;
                OnPropertyChanged(nameof(ShortName));
            }
        }

        private string _LongName;

        public string LongName
        {
            get => _LongName;
            set
            {
                if(value == _LongName)
                    return;
                _LongName = value;
                OnPropertyChanged(nameof(LongName));
            }
        }

        
    }
}
