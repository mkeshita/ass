
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using norsu.ass.Models;
using norsu.ass.Network;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;

namespace norsu.ass.Server.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        public const int LOGIN = 5, HOME=0,RATINGS=1,REVIEWS=2,USERS=3,ADMIN=4;

        private MainViewModel()
        {
            Messenger.Default.AddListener(Messages.LoggedIn, () =>
            {
                Screen = HOME;
                HasLoggedIn = true;
            });
            
            Messenger.Default.AddListener(Messages.ServerFound, () =>
            {
                OnPropertyChanged(nameof(ServerOffline));
            });
            
            NetworkComms.AppendGlobalIncomingPacketHandler<SettingsViewModel>("settings",
                (header, connection, settings) =>
                {
                    Settings = settings;
                    IsSavingSettings = false;
                });
            
            Messenger.Default.AddListener(Messages.Logout,() =>
            {
                Screen = LOGIN;
                HasLoggedIn = false;
            });
            
            DownloadData();
        }

        private ICommand _runExternalCommand;

        public ICommand RunExternalCommand => _runExternalCommand ?? (_runExternalCommand = new DelegateCommand<string>(
        cmd =>
        {
            Process.Start(cmd);
        }));

        private string _NetworkStatus;

        public string NetworkStatus
        {
            get => _NetworkStatus;
            set
            {
                if(value == _NetworkStatus)
                    return;
                _NetworkStatus = value;
                OnPropertyChanged(nameof(NetworkStatus));
            }
        }

        private ChangePasswordViewModel _ChangePassword;

        public ChangePasswordViewModel ChangePassword
        {
            get => _ChangePassword;
            set
            {
                if(value == _ChangePassword)
                    return;
                _ChangePassword = value;
                OnPropertyChanged(nameof(ChangePassword));
                OnPropertyChanged(nameof(ShowChangePassword));
            }
        }

        private bool _ShowChangePassword;

        public bool ShowChangePassword
        {
            get => ChangePassword!=null;
            set
            {
                if(value == _ShowChangePassword)
                    return;
                _ShowChangePassword = value;
                OnPropertyChanged(nameof(ShowChangePassword));
            }
        }
        
        private ICommand _changePasswordCommand;

        public ICommand ChangePasswordCommand =>
            _changePasswordCommand ?? (_changePasswordCommand = new DelegateCommand(
                d =>
                {
                    ChangePassword = new ChangePasswordViewModel();
                }));

        private ICommand _cancelChangePasswordCommand;

        public ICommand CancelChangePasswordCommand =>
            _cancelChangePasswordCommand ?? (_cancelChangePasswordCommand = new DelegateCommand(
                d =>
                {
                    ChangePassword = null;
                    GC.Collect();
                }));

        private ICommand _acceptChangePasswordCommand;

        public ICommand AcceptChangePasswordCommand =>
            _acceptChangePasswordCommand ?? (_acceptChangePasswordCommand = new DelegateCommand(
                async d =>
                {
                    if (! await ChangePassword.Process()) return;
                    ChangePassword = null;
                    GC.Collect();
                }));

        private SettingsViewModel _Setting;

        public SettingsViewModel Settings
        {
            get => _Setting;
            set
            {
                if(value == _Setting)
                    return;
                _Setting = value;
                if (_Setting != null)
                    _Setting.PropertyChanged += (sender, args) =>
                    {
                        IsSavingSettings = false;
                    };
                OnPropertyChanged(nameof(Settings));
            }
        }

        private bool _IsSavingSettings;

        public bool IsSavingSettings
        {
            get => _IsSavingSettings;
            set
            {
                if(value == _IsSavingSettings)
                    return;
                _IsSavingSettings = value;
                OnPropertyChanged(nameof(IsSavingSettings));
            }
        }

        private ICommand _saveSettingsCommand;

        public ICommand SaveSettingsCommand => _saveSettingsCommand ?? (_saveSettingsCommand = new DelegateCommand(d =>
        {
            IsSavingSettings = true;
            Client.Send("settings",Settings);
        }));

        private void DownloadData()
        {
            //OfficeViewModel.Instance.DownloadData();
            //NetworkStatus = "Downloading users...";
            //Client.GetUsers();
        }

        private bool _UsersDownloaded;

        public bool UsersDownloaded
        {
            get => _UsersDownloaded;
            set
            {
                if(value == _UsersDownloaded)
                    return;
                _UsersDownloaded = value;
                OnPropertyChanged(nameof(UsersDownloaded));
            }
        }
       
        private static Dictionary<int,bool> UserPages = new Dictionary<int, bool>();
        private void GetUsersHandler(PacketHeader packetheader, Connection connection, GetUsersResult res)
        {
            foreach (var usr in res.Users)
            {
                var user = User.Cache.FirstOrDefault(x => x.Id == usr.Id);
                if (user == null)
                {
                    user = new User();
                    user.Defer = true;
                    user.Save();
                    user.ChangeId(usr.Id);
                }

                user.Access = (AccessLevels) usr.Access;
                user.Course = usr.Description;
                user.Firstname = usr.Firstname;
                user.Lastname = usr.Lastname;
                user.Username = usr.Username;
                user.Password = usr.Password;
                user.Save();
               // Client.GetPicture(user.Id);
            }
            
            for (int i = 0; i < res.Pages; i++)
            {
                if (!UserPages.ContainsKey(i))
                    UserPages.Add(i, false);
            }

            UserPages[res.Page] = true;

            if (UserPages.Count == res.Pages)
            {
                UsersDownloaded = true;
            }
        }

        private static MainViewModel _instance;
        public static MainViewModel Instance => _instance ?? (_instance = new MainViewModel());
        
        private int _Screen = LOGIN;

        public int Screen
        {
            get => _Screen;
            set
            {
                if(value == _Screen)
                    return;
                _Screen = value;
                OnPropertyChanged(nameof(Screen));
            }
        }

        private bool _HasLoggedIn = false;

        public bool HasLoggedIn
        {
            get => _HasLoggedIn;
            set
            {
                if(value == _HasLoggedIn)
                    return;
                _HasLoggedIn = value;
                OnPropertyChanged(nameof(HasLoggedIn));
            }
        }
        
        private bool _ServerOffline;

        public bool ServerOffline
        {
            get => Client.Server == null;
            set
            {
                if(value == _ServerOffline)
                    return;
                _ServerOffline = value;
                OnPropertyChanged(nameof(ServerOffline));
            }
        }


        public static void ShowToast(string text)
        {
            
        }
    }
}
