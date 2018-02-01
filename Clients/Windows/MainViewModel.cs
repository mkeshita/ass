
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            Messenger.Default.AddListener<UserPicture>(Messages.PictureReceived, pic =>
            {
                var usr = User.Cache.FirstOrDefault(x => x.Id == pic.UserId);
                if (usr == null)
                    return;
                usr.Picture = pic.Picture;
            });

            NetworkComms.AppendGlobalIncomingPacketHandler<GetUsersResult>(GetUsersResult.Header, GetUsersHandler);
            
            DownloadData();
        }

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
        
        private async void DownloadData()
        {
            OfficeViewModel.Instance.DownloadData();
            NetworkStatus = "Downloading users...";
            await Client.GetUsers(-1);
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
        
        private ICommand _downloadUsersCommand;
        private bool _downloadingUsers;
        public ICommand DownloadUsersCommand => _downloadUsersCommand ?? (_downloadUsersCommand = new DelegateCommand(
        async d =>
        {
            if (_downloadingUsers) return;
            _downloadingUsers = true;
            foreach (var userPage in UserPages)
            {
                if (!userPage.Value)
                    await Client.GetUsers(userPage.Key);
            }
            _downloadingUsers = false;
        },d=>!_downloadingUsers && !UsersDownloaded));
        
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
                Client.GetPicture(user.Id);
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
        
        private int _Screen = HOME;

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

        private bool _HasLoggedIn = true;

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
        
    }
}
