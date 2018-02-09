using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using norsu.ass.Models;
using norsu.ass.Network;
using Office = norsu.ass.Models.Office;

namespace norsu.ass.Server.ViewModels
{
    class UsersViewModel : ViewModelBase
    {
        private UsersViewModel() { }
        private static UsersViewModel _instance;
        public static UsersViewModel Instance => _instance ?? (_instance = new UsersViewModel());

        private ListCollectionView _users;

        public ListCollectionView Users
        {
            get
            {
                if (_users != null) return _users;
                _users = new ListCollectionView(Models.User.Cache);
                _users.Filter = FilterUser;
                return _users;
            }
        }

        private bool FilterUser(object o)
        {
            if (!(o is Models.User u)) return false;
            return u.Access > AccessLevels.Student;
        }

        private ICommand _addCommand;

        public ICommand AddCommand => _addCommand ?? (_addCommand = new DelegateCommand(d =>
        {
            NewItem = new User()
            {
                Access = AccessLevels.OfficeAdmin,
                IsProcessing = false,
                EditMode = true,
                Picture = ImageProcessor.Generate(),
            };
            ShowNewItem = true;
        },d=>LoginViewModel.Instance.User?.IsSuperAdmin??false));

        private Models.User _NewItem;

        public Models.User NewItem
        {
            get => _NewItem;
            set
            {
                if(value == _NewItem)
                    return;
                _NewItem = value;
                OnPropertyChanged(nameof(NewItem));
            }
        }

        private bool _ShowNewItem;

        public bool ShowNewItem
        {
            get => _ShowNewItem;
            set
            {
                if(value == _ShowNewItem)
                    return;
                _ShowNewItem = value;
                OnPropertyChanged(nameof(ShowNewItem));
            }
        }

        private ICommand _cancelAddCommand;

        public ICommand CancelAddCommand => _cancelAddCommand ?? (_cancelAddCommand = new DelegateCommand(d =>
        {
            NewItem = null;
            ShowNewItem = false;
            GC.Collect();
        }));

        private ICommand _acceptNewCommand;

        public ICommand AcceptNewCommand => _acceptNewCommand ?? (_acceptNewCommand = new DelegateCommand(
        async d =>
        {
            if (string.IsNullOrEmpty(NewItem.Username) ||
                string.IsNullOrEmpty(NewItem.Firstname))
            {
                MainViewModel.ShowToast("Adding new user failed. Incomplete data.");
                return;
            }

            NewItem.EditMode = false;
            NewItem.IsProcessing = true;

            var user = new UserInfo()
            {
                Username = NewItem.Username,
                Firstname = NewItem.Firstname,
                Access =(int) (NewItem.Access??AccessLevels.OfficeAdmin),
                
            };
            
            var res = await Client.SaveUser(user);

            if (res?.Success ?? false)
            {
                NewItem.Save();
                try
                {
                    NewItem.ChangeId(res.Id);
                }
                catch (Exception e)
                {
                    //
                }
                ShowNewItem = false;
            }
            else
            {
                ShowNewItem = true;
                NewItem.EditMode = true;
                MainViewModel.ShowToast("Adding new user failed!");
            }

            NewItem.IsProcessing = false;
        },
        d => !(string.IsNullOrEmpty(NewItem.Username) ||
                string.IsNullOrEmpty(NewItem.Firstname))));


        private readonly Queue<Task> _updateTasks = new Queue<Task>();
        private bool _updateStarted;
        private readonly object _updateLock = new object();
        private ICommand _updateCommand;

        public ICommand UpdateCommand => _updateCommand ?? (_updateCommand = new DelegateCommand<Models.User>(ofc =>
        {
            ofc.IsProcessing = true;
            lock (_updateLock)
                _updateTasks.Enqueue(new Task(async o =>
                {
                    if (!(o is User of))
                        return;

                    var user = new UserInfo()
                    {
                        Username = ofc.Username,
                        Firstname = ofc.Firstname,
                        Access = (int) (ofc.Access ?? AccessLevels.OfficeAdmin),
                        Id = ofc.Id,
                    };

                    of.IsProcessing = true;
                    SaveUserResult res = null;
                    while (!(res?.Success ?? false))
                        res = await Client.SaveUser(user);
                    of.Save();
                    of.IsProcessing = false;
                }, ofc));

            ProcessUpdates();
        }, o => (LoginViewModel.Instance.User?.IsSuperAdmin??false) && o.CanSave()));

        private ICommand _ToggleAccessCommand;

        public ICommand ToggleAccessCommand => _ToggleAccessCommand ?? (_ToggleAccessCommand = new DelegateCommand<Models.User>(ofc =>
        {
            ofc.IsProcessing = true;
            lock (_updateLock)
                _updateTasks.Enqueue(new Task(async o =>
                {
                    if (!(o is User of))
                        return;

                    var user = new UserInfo()
                    {
                        Username = ofc.Username,
                        Firstname = ofc.Firstname,
                        Access = (int) (ofc.Access==AccessLevels.OfficeAdmin ? AccessLevels.SuperAdmin : AccessLevels.OfficeAdmin),
                        Id = ofc.Id,
                    };

                    of.IsProcessing = true;
                    SaveUserResult res = null;
                    while (!(res?.Success ?? false))
                        res = await Client.SaveUser(user);
                    of.Access = ofc.Access == AccessLevels.OfficeAdmin
                        ? AccessLevels.SuperAdmin
                        : AccessLevels.OfficeAdmin;
                    of.Save();
                    of.IsProcessing = false;
                }, ofc));

            ProcessUpdates();
        }, o => o.CanSave() && o.Id!=LoginViewModel.Instance.User?.Id));


        private ICommand _deleteCommand;

        public ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new DelegateCommand<Models.User>(d =>
        {
            d.IsProcessing = true;
            lock (_updateLock)
                _updateTasks.Enqueue(new Task(async o =>
                {
                    if (!(o is User of))
                        return;
                    of.IsProcessing = true;
                    DeleteUserResult res = null;
                    while (!(res?.Success ?? false))
                        res = await Client.DeleteUser(of.Id);
                    of.Delete();
                }, d));

            ProcessUpdates();
        }, d => d.CanDelete()));

        private void ProcessUpdates()
        {
            if (_updateStarted)
                return;
            _updateStarted = true;

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    //IsProcessing = true;
                    Task task;
                    lock (_updateLock)
                    {
                        if (_updateTasks.Count == 0)
                            break;
                        task = _updateTasks.Dequeue();
                    }

                    if (task == null)
                        break;

                    task.Start();
                    task.Wait();

                }
                // IsProcessing = false;
                _updateStarted = false;
            });
        }

        private ICommand _changePictureCommand;

        public ICommand ChangePictureCommand => _changePictureCommand ?? (_changePictureCommand =
            new DelegateCommand<User>(
                d =>
                {
                    var image = ImageProcessor.GetPicture(256);
                    if (image == null)
                        return;
                    if (d.Id == 0)
                    {
                        d.Picture = image;
                        return;
                    }

                    d.IsProcessing = true;

                    lock (_updateLock)
                        _updateTasks.Enqueue(new Task(async o =>
                        {
                            if (!(o is User of))
                                return;
                            of.IsProcessing = true;
                            SetPictureResult res = null;
                            while (!(res?.Success ?? false))
                                res = await Client.SetPicture(of.Id,
                                    image);
                            of.Update(nameof(User.Picture), image);
                            of.IsProcessing = false;
                        }, d));

                    ProcessUpdates();
                }));

        private ICommand _showAddOfficeCommand;
        private User _addOfficeUser;
        public ICommand AddOfficeCommand => _showAddOfficeCommand ?? (_showAddOfficeCommand = new DelegateCommand<User>(
        d =>
        {
            _addOfficeUser = d;
            ShowAddOffice = true;
            Offices.Filter = FilterOffice;
        },d=>!d?.IsSuperAdmin??false));

        private bool _ShowAddOffice;

        public bool ShowAddOffice
        {
            get => _ShowAddOffice;
            set
            {
                if(value == _ShowAddOffice)
                    return;
                _ShowAddOffice = value;
                OnPropertyChanged(nameof(ShowAddOffice));
            }
        }

        private ListCollectionView _offices;

        public ListCollectionView Offices
        {
            get
            {
                if (_offices != null) return _offices;
                _offices = new ListCollectionView(Office.Cache);
                _offices.Filter = FilterOffice;
                return _offices;
            }
        }

        private bool FilterOffice(object o)
        {
            if (_addOfficeUser == null)
                return false;
            if (!(o is Office office)) return false;
            return !_addOfficeUser.Offices.Any(x => x.Id == office.Id);
        }

        private ICommand _cancelAddOfficeCommand;

        public ICommand CancelAddOfficeCommand =>
            _cancelAddOfficeCommand ?? (_cancelAddOfficeCommand = new DelegateCommand(
                d =>
                {
                    ShowAddOffice = false;
                }));

        private ICommand _acceptAddOfficeCommand;

        public ICommand AcceptAddOfficeCommand =>
            _acceptAddOfficeCommand ?? (_acceptAddOfficeCommand = new DelegateCommand<Office>(
                async d =>
                {
                    ShowAddOffice = false;
                    _addOfficeUser.IsAddingOffice = true;
                    var res = await Client.AddOfficeAdmin(d.Id, _addOfficeUser.Id);
                    _addOfficeUser.IsAddingOffice = false;
                    if (res?.Success ?? false)
                    {
                        var office = new OfficeAdmin()
                        {
                            UserId = _addOfficeUser.Id,
                            OfficeId = d.Id,

                        };
                        office.Save();
                        _addOfficeUser.RefreshOffices();
                    }
                    
                    
                },d=>d!=null));
    }
}
