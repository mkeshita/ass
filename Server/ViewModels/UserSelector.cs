using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using norsu.ass.Models;

namespace norsu.ass.Server.ViewModels
{
    class UserSelector : ViewModelBase
    {
        private AccessLevels? _Access;
        
        public AccessLevels? Access
        {
            get => _Access;
            set
            {
                if(value == null)
                {
                    OnPropertyChanged(nameof(Access));
                    return;
                }
                if(value == _Access)
                    return;
                _Access = value;
                OnPropertyChanged(nameof(Access));
            }
        }

        private long _UserId;

        public long Id
        {
            get => _UserId;
            set
            {
                if(value == _UserId)
                    return;
                _UserId = value;
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(IsNew));
                if (Id==0)
                {
                    Access = AccessLevels.OfficeAdmin;
                }
            }
        }
        
        public bool IsNew => Id == 0;
        

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
                ClearFields();
                if (string.IsNullOrEmpty(value)) return;
                var usr = User.Cache.FirstOrDefault(x => x.Username.ToLower() == value.ToLower() && !x.IsAnnonymous);
                if (usr == null) return;
                Firstname = usr.Firstname;
                Picture = usr.Picture;
                Course = usr.Course;
                Access = usr.Access;
                Id = usr.Id;
            }
        }

        private void ClearFields()
        {
            if (Id == 0) return;
            Picture = null;
            Firstname = "";
            Course = "";
            Id = 0;
        }
        
        private byte[] _Picture;
        public byte[] Picture
        {
            get => _Picture;
            set
            {
                if(value == _Picture)
                    return;
                _Picture = value;
                OnPropertyChanged(nameof(Picture));
            }
        }
        
        public bool HasPicture => Picture?.Length > 0;

        private string _Firstname;

        public string Firstname
        {
            get => _Firstname;
            set
            {
                if(value == _Firstname)
                    return;
                _Firstname = value;
                OnPropertyChanged(nameof(Firstname));
            }
        }
        
        private string _Course;

        public string Course
        {
            get => _Course;
            set
            {
                if(value == _Course)
                    return;
                _Course = value;
                OnPropertyChanged(nameof(Course));
            }
        }

    }
}
