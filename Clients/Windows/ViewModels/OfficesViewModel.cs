using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using norsu.ass.Network;
using Office = norsu.ass.Models.Office;

namespace norsu.ass.Server.ViewModels
{
    class OfficesViewModel : ViewModelBase
    {
        private OfficesViewModel()
        {
            Messenger.Default.AddListener(Messages.Logout, () =>
            {
                ShowNewItem = false;
                NewItem = null;
                GC.Collect();
            });

            Messenger.Default.AddListener(Messages.DatabaseRefreshed, () =>
            {
                _offices = null;
                OnPropertyChanged(nameof(Offices));
            });
        }
        private static OfficesViewModel _instance;
        public static OfficesViewModel Instance => _instance ?? (_instance = new OfficesViewModel());

        private ListCollectionView _offices;

        public ListCollectionView Offices
        {
            get
            {
                if (_offices != null) return _offices;
                _offices = new ListCollectionView(Models.Office.Cache);
                return _offices;
            }
        }
        
        private static Models.Office _NewItem;

        public Models.Office NewItem
        {
            get => _NewItem;
            set
            {
                _NewItem = value;
                OnPropertyChanged(nameof(NewItem));
            }
        }

        private ICommand _cancelAddCommand;

        public ICommand CancelAddCommand => _cancelAddCommand ?? (_cancelAddCommand = new DelegateCommand(d =>
        {
            ShowNewItem = false;
            NewItem = null;
            GC.Collect();
        }));

        private ICommand _addCommand;

        public ICommand AddCommand => _addCommand ?? (_addCommand = new DelegateCommand(d =>
        {
            CanAcceptNew = true;
            NewItem = new Office
            {
                IsProcessing = false,
                EditMode = true
            };
            ShowNewItem = true;
        },d=> LoginViewModel.Instance.User?.IsSuperAdmin??false));

        private bool _ShowNewItem;

        public bool ShowNewItem
        {
            get => _ShowNewItem;
            set
            {
                _ShowNewItem = value;
                OnPropertyChanged(nameof(ShowNewItem));
            }
        }

        private ICommand _acceptNewCommand;

        public ICommand AcceptNewCommand => _acceptNewCommand ?? (_acceptNewCommand = new DelegateCommand(
        async d =>
        {
            if (string.IsNullOrEmpty(NewItem.ShortName) || string.IsNullOrEmpty(NewItem.LongName)) return;

            NewItem.EditMode = false;
            NewItem.IsProcessing = true;
            
            var res = await Client.SaveOffice(0, NewItem.ShortName, NewItem.LongName);
            
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
                MainViewModel.ShowToast("Adding new office failed!");
            }

            NewItem.IsProcessing = false;
        },d=> !(string.IsNullOrEmpty(NewItem.ShortName) || string.IsNullOrEmpty(NewItem.LongName))));

        private bool _CanAcceptNew;

        public bool CanAcceptNew
        {
            get => _CanAcceptNew;
            set
            {
                if(value == _CanAcceptNew)
                    return;
                _CanAcceptNew = value;
                OnPropertyChanged(nameof(CanAcceptNew));
            }
        }

        //private bool _IsProcessing;

        //public bool IsProcessing
        //{
        //    get => _IsProcessing;
        //    set
        //    {
        //        if(value == _IsProcessing)
        //            return;
        //        _IsProcessing = value;
        //        OnPropertyChanged(nameof(IsProcessing));
        //    }
        //}

        private string _Status;

        public string Status
        {
            get => _Status;
            set
            {
                if(value == _Status)
                    return;
                _Status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        
        private readonly Queue<Task> _updateTasks = new Queue<Task>();
        private bool _updateStarted;
        private readonly object _updateLock = new object();
        private ICommand _updateCommand;
        
        public ICommand UpdateCommand => _updateCommand ?? (_updateCommand = new DelegateCommand<Models.Office>(ofc =>
        {
            ofc.IsProcessing = true;
            lock(_updateLock)
            _updateTasks.Enqueue(new Task(async o =>
            {
                if (!(o is Office of)) return;
                of.IsProcessing = true;
                SaveOfficeResult res = null;
                while (!(res?.Success ?? false))
                    res = await Client.SaveOffice(of.Id, of.ShortName, of.LongName);
                of.Save();
                of.IsProcessing = false;
            },ofc));
            
            ProcessUpdates();
        },o=>o.CanSave()));

        private ICommand _deleteCommand;

        public ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new DelegateCommand<Models.Office>(d =>
        {
            d.IsProcessing = true;
            lock(_updateLock)
                _updateTasks.Enqueue(new Task(async o =>
                {
                    if (!(o is Office of)) return;
                    of.IsProcessing = true;
                    DeleteOfficeResult res = null;
                    while (!(res?.Success ?? false))
                        res = await Client.DeleteOffice(of.Id);
                    of.Delete();
                },d));

            ProcessUpdates();
        },d=>d.CanDelete()));

        private void ProcessUpdates()
        {
            if (_updateStarted) return;
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

                    if (task == null) break;
                    
                    task.Start();
                    task.Wait();
                    
                }
               // IsProcessing = false;
                _updateStarted = false;
            });
        }

        private ICommand _changePictureCommand;

        public ICommand ChangePictureCommand => _changePictureCommand ?? (_changePictureCommand = new DelegateCommand<Office>(
        d =>
        {
            var image = ImageProcessor.GetPicture(256);
            if (image == null) return;
            if (d.Id == 0)
            {
                d.Picture = image;
                return;
            }

            d.IsProcessing = true;
            
            lock(_updateLock)
                _updateTasks.Enqueue(new Task(async o =>
                {
                    if (!(o is Office of)) return;
                    of.IsProcessing = true;
                    SetOfficePictureResult res = null;
                    while (!(res?.Success ?? false))
                        res = await Client.SetOfficePicture(of.Id,image);
                    of.Update(nameof(Office.Picture),image);
                    of.IsProcessing = false;
                },d));
            
            ProcessUpdates();
        }));
    }
}