using System.ComponentModel;
using ProtoBuf;

namespace norsu.ass.Server.ViewModels
{
    
    [ProtoContract]
    class SettingsViewModel : INotifyPropertyChanged
    {
        public SettingsViewModel() { }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if(awooo.Context!=null)
            awooo.Context.Post(d =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }, null);
        }

        private bool _AndroidRegistration;
        [ProtoMember(1)]
        public bool AndroidRegistration
        {
            get => _AndroidRegistration;
            set
            {
                if(value == _AndroidRegistration)
                    return;
                _AndroidRegistration = value;
                OnPropertyChanged(nameof(AndroidRegistration));
            }
        }

        private bool _AllowAnonymous;

        [ProtoMember(2)]
        public bool AllowAnonymous
        {
            get => _AllowAnonymous;
            set
            {
                if(value == _AllowAnonymous)
                    return;
                _AllowAnonymous = value;
                OnPropertyChanged(nameof(AllowAnonymous));
            }
        }

        private bool _AllowPrivate;

        [ProtoMember(3)]
        public bool AllowPrivate
        {
            get => _AllowPrivate;
            set
            {
                if(value == _AllowPrivate)
                    return;
                _AllowPrivate = value;
                OnPropertyChanged(nameof(AllowPrivate));
            }
        }

        private bool _OfficeAdminCanDeleteSuggestions;

        [ProtoMember(4)]
        public bool OfficeAdminCanDeleteSuggestions
        {
            get => _OfficeAdminCanDeleteSuggestions;
            set
            {
                if(value == _OfficeAdminCanDeleteSuggestions)
                    return;
                _OfficeAdminCanDeleteSuggestions = value;
                OnPropertyChanged(nameof(OfficeAdminCanDeleteSuggestions));
            }
        }

        private bool _OfficeAdminCanSeePrivate;

        [ProtoMember(5)]
        public bool OfficeAdminCanSeePrivate
        {
            get => _OfficeAdminCanSeePrivate;
            set
            {
                if(value == _OfficeAdminCanSeePrivate)
                    return;
                _OfficeAdminCanSeePrivate = value;
                OnPropertyChanged(nameof(OfficeAdminCanSeePrivate));
            }
        }

        private bool _OfficeAdminReplyAs;

        [ProtoMember(6)]
        public bool OfficeAdminReplyAs
        {
            get => _OfficeAdminReplyAs;
            set
            {
                if(value == _OfficeAdminReplyAs)
                    return;
                _OfficeAdminReplyAs = value;
                OnPropertyChanged(nameof(OfficeAdminReplyAs));
            }
        }

        private int _SuggestionTitleMinimum;

        [ProtoMember(7)]
        public int SuggestionTitleMinimum
        {
            get => _SuggestionTitleMinimum;
            set
            {
                if(value == _SuggestionTitleMinimum)
                    return;
                _SuggestionTitleMinimum = value;
                OnPropertyChanged(nameof(SuggestionTitleMinimum));
            }
        }

        private int _SuggestionTitleMaximum;

        [ProtoMember(8)]
        public int SuggestionTitleMaximum
        {
            get => _SuggestionTitleMaximum;
            set
            {
                if(value == _SuggestionTitleMaximum)
                    return;
                _SuggestionTitleMaximum = value;
                OnPropertyChanged(nameof(SuggestionTitleMaximum));
            }
        }

        private int _SuggestionBodyMinimum;

        [ProtoMember(9)]
        public int SuggestionBodyMinimum
        {
            get => _SuggestionBodyMinimum;
            set
            {
                if(value == _SuggestionBodyMinimum)
                    return;
                _SuggestionBodyMinimum = value;
                OnPropertyChanged(nameof(SuggestionBodyMinimum));
            }
        }

        private int _SuggestionBodyMaximum;

        [ProtoMember(10)]
        public int SuggestionBodyMaximum
        {
            get => _SuggestionBodyMaximum;
            set
            {
                if(value == _SuggestionBodyMaximum)
                    return;
                _SuggestionBodyMaximum = value;
                OnPropertyChanged(nameof(SuggestionBodyMaximum));
            }
        }
    }
}
