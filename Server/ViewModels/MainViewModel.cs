using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Devcorner.NIdenticon;
using Devcorner.NIdenticon.BlockGenerators;
using Devcorner.NIdenticon.BrushGenerators;
using MaterialDesignThemes.Wpf;
using norsu.ass.Models;
using norsu.ass.Server.Views;

namespace norsu.ass.Server.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        private MainViewModel() { }

        private static MainViewModel _instance;
        public static MainViewModel Instance => _instance ?? (_instance = new MainViewModel());

        private ICommand _showAccountCommand;

        public ICommand ShowAccountCommand => _showAccountCommand ?? (_showAccountCommand = new DelegateCommand(d =>
        {
            SidebarIndex = 0;
        }));

        private ICommand _showOfficesCommand;
        public ICommand ShowOfficesCommand => _showOfficesCommand ?? (_showOfficesCommand = new DelegateCommand(d =>
        {
            SidebarIndex = 2;
        }));

        private ICommand _showSettingsCommand;

        public ICommand ShowSettingsCommand => _showSettingsCommand ?? (_showSettingsCommand = new DelegateCommand(d =>
        {
            SidebarIndex = 1;
        }));

        private ICommand _changePasswordCommand;

        public ICommand ChangePasswordCommand =>
            _changePasswordCommand ??
            (_changePasswordCommand = new DelegateCommand(d => DialogHost.Show(new ChangePasswordDialog(CurrentUser))));

        private ICommand _addOfficeCommand;

        public ICommand AddOfficeCommand => _addOfficeCommand ?? (_addOfficeCommand = new DelegateCommand(async d =>
        {
            var offce = new NewOfficeViewModel();
            await DialogHost.Show(offce, "DialogHost", null,
                (sender, args) =>
                {
                    if (args.IsCancelled)
                        return;

                    var ofc = new Office()
                    {
                        ShortName = offce.ShortName,
                        LongName = offce.LongName,
                    };
                    ofc.Save();
                });

            //  if ((bool)result) return;

        }));

        private ICommand _logoutCommand;

        public ICommand LogoutCommand => _logoutCommand ?? (_logoutCommand = new DelegateCommand(d =>
        {
            CurrentUser = null;
            SelectedIndex = 0;
        }));

        private ICommand _loginCommand;

        public ICommand LoginCommand => _loginCommand ?? (_loginCommand = new DelegateCommand<PasswordBox>(d =>
        {
            var user = Models.User.Cache.FirstOrDefault(x => x.Username?.ToLower() == Username.ToLower());
            if (user == null && Models.User.Cache.Count(x => x.Access == User.AccessLevels.SuperAdmin) == 0)
            {
                var gen = new IdenticonGenerator()
                    .WithBlocks(7, 7)
                    .WithSize(128, 128)
                    .WithBlockGenerators(IdenticonGenerator.ExtendedBlockGeneratorsConfig)
                    .WithBackgroundColor(Color.White)
                    .WithBrushGenerator(new StaticColorBrushGenerator(Color.Red));
                
                using (var pic = gen.Create(Username+DateTime.Now.Ticks))
                {
                    using (var stream = new MemoryStream())
                    {
                            pic.Save(stream, ImageFormat.Jpeg);
                        user = new User()
                        {
                            Username = Username,
                            Password = d.Password,
                            Access = User.AccessLevels.SuperAdmin,
                            Picture = stream.ToArray(),
                        };
                        user.Save();
                    }
                }
            }

            if (user == null || user?.Password != d.Password)
            {
                MessageBox.Show("Invalid username or password.", "Login Failed", 
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
                d.SelectAll();
                d.Focus();
                return;
            }

            CurrentUser = user;
            
            d.Password = string.Empty;
            Username = "";
            SelectedIndex = 1;
        }));

        private Models.User _CurrentUser;

        public Models.User CurrentUser
        {
            get => _CurrentUser;
            set
            {
                if(value == _CurrentUser)
                    return;
                _CurrentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
            }
        }

        private int _SidebarIndex = 2;

        public int SidebarIndex
        {
            get => _SidebarIndex;
            set
            {
                if(value == _SidebarIndex)
                    return;
                _SidebarIndex = value;
                OnPropertyChanged(nameof(SidebarIndex));
            }
        }
        
        private int _SelectedIndex = 0;

        public int SelectedIndex
        {
            get => _SelectedIndex;
            set
            {
                if(value == _SelectedIndex)
                    return;
                _SelectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
            }
        }

        private string _Password;

        public string Password
        {
            get => _Password;
            set
            {
                if(value == _Password)
                    return;
                _Password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

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
            }
        }

        private ListCollectionView _offices;

        public ListCollectionView Offices
        {
            get
            {
                if (_offices != null) return _offices;                
                _offices = new ListCollectionView(Office.Cache);
                return _offices;
            }
        }
    }
}
