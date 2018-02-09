
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

            Messenger.Default.AddListener<ReceivedFile>(Messages.DatabaseDownloaded, async file =>
            {
                if (DownloadCompleted)
                    return;

                file.SaveFileToDisk(awooo.DataSource);

                User.ClearPasswords();

                StatusText = "CONNECTION SUCCESSFULL";
                DownloadSuccess = true;

                await TaskEx.Delay(1111);

                DownloadCompleted = true;
            });
            
            Messenger.Default.AddListener(Messages.PartialDataReceived, () =>
            {
                LastDataReceived = DateTime.Now;
            });
            
            Messenger.Default.AddListener(Messages.ServerShutdown,async () =>
            {
                StatusText = "DISCONNECTED FROM SERVER";
                DownloadError = true;
                DownloadSuccess = false;
                DownloadCompleted = false;
                _downloadInitiated = false;

                await TaskEx.Delay(4444);

                Download();
            });
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

        private bool _DownloadCompleted;

        public bool DownloadCompleted
        {
            get => _DownloadCompleted;
            set
            {
                if(value == _DownloadCompleted)
                    return;
                _DownloadCompleted = value;
                OnPropertyChanged(nameof(DownloadCompleted));
            }
        }

        private string _StatusText= "CONNECTING TO SERVER...";

        public string StatusText
        {
            get => _StatusText;
            set
            {
                if(value == _StatusText)
                    return;
                _StatusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        private bool _DownloadError;

        public bool DownloadError
        {
            get => _DownloadError;
            set
            {
                if(value == _DownloadError)
                    return;
                _DownloadError = value;
                OnPropertyChanged(nameof(DownloadError));
            }
        }

        private bool _DownloadSuccess;

        public bool DownloadSuccess
        {
            get => _DownloadSuccess;
            set
            {
                if(value == _DownloadSuccess)
                    return;
                _DownloadSuccess = value;
                OnPropertyChanged(nameof(DownloadSuccess));
            }
        }
        private DateTime LastDataReceived { get; set; }
        private bool _downloadInitiated;
        public async void Download()
        {
            if (DownloadCompleted) return;
            
            if (_downloadInitiated) return;
            _downloadInitiated = true;

            while (true)
            {
                DownloadError = false;
                DownloadSuccess = false;
                StatusText = "CONNECTING TO SERVER...";
                
                var res = await Client.SendAsync(new Database());

                if (res)
                {
                    LastDataReceived = DateTime.Now;
                    while ((DateTime.Now - LastDataReceived).TotalMilliseconds < 1111)
                        await TaskEx.Delay(111);

                    if (DownloadSuccess || DownloadCompleted)
                        return;

                    StatusText = "CONNECTION TIMEOUT";
                }
                else
                {
                    StatusText = "CAN NOT FIND SERVER";
                }
                
                DownloadError = true;
                await TaskEx.Delay(3333);
                StatusText = "RETRYING IN FEW SECONDS";
                await TaskEx.Delay(7777);
            }
        }

        private ICommand _DownloadCommand;

        public ICommand DownloadCommand => _DownloadCommand ?? (_DownloadCommand = new DelegateCommand(d =>
        {
            Download();
        }));

        private ICommand _exitCommand;

        public ICommand ExitCommand => _exitCommand ?? (_exitCommand = new DelegateCommand(d =>
        {
            Application.Current.Shutdown();
        }));
    }
}
