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
                OnPropertyChanged(nameof(HasError));
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

        public bool HasError => string.IsNullOrEmpty(ShortName);
    }
}
