using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Devcorner.NIdenticon;
using Devcorner.NIdenticon.BlockGenerators;
using Devcorner.NIdenticon.BrushGenerators;
using norsu.ass.Models;

namespace norsu.ass.Server.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        private MainViewModel() { }

        private static MainViewModel _instance;
        public static MainViewModel Instance => _instance ?? (_instance = new MainViewModel());

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

        
    }
}
