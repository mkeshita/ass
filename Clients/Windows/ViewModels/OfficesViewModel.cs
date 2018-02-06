using System;
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
        private OfficesViewModel() { }
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

        private Models.Office _NewItem;

        public Models.Office NewItem
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

        private ICommand _cancelAddCommand;

        public ICommand CancelAddCommand => _cancelAddCommand ?? (_cancelAddCommand = new DelegateCommand(d =>
        {
            ShowNewItem = false;
            NewItem = null;
        }));

        private ICommand _addCommand;

        public ICommand AddCommand => _addCommand ?? (_addCommand = new DelegateCommand(d =>
        {
            CanAcceptNew = true;
            NewItem = new Office();
            ShowNewItem = true;
        }));

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

        private ICommand _acceptNewCommand;

        public ICommand AcceptNewCommand => _acceptNewCommand ?? (_acceptNewCommand = new DelegateCommand(
        async d =>
        {
            if (string.IsNullOrEmpty(NewItem.ShortName) || string.IsNullOrEmpty(NewItem.LongName)) return;
            CanAcceptNew = false;
            
            var res = await Client.SaveOffice(0, NewItem.ShortName, NewItem.LongName);
            CanAcceptNew = true;
            if (res?.Success ?? false)
            {
                NewItem.Save();
                try
                {
                    NewItem.Update("Id", res.Id);
                }
                catch (Exception e)
                {
                    //
                }

                ShowNewItem = false;
            }
            else
            {
                MainViewModel.ShowToast("Adding new office failed!");
            }
        }));

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

        private bool _IsProcessing;

        public bool IsProcessing
        {
            get => _IsProcessing;
            set
            {
                if(value == _IsProcessing)
                    return;
                _IsProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
            }
        }

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
            lock(_updateLock)
            _updateTasks.Enqueue(new Task(async o =>
            {
                if (!(o is Office of)) return;
                SaveOfficeResult res = null;
                while (!(res?.Success ?? false))
                    res = await Client.SaveOffice(of.Id, of.ShortName, of.LongName);
                of.Save();
            },ofc));
            
            ProcessUpdates();
        },o=>o.CanSave()));

       

        private void ProcessUpdates()
        {
            if (_updateStarted) return;
            _updateStarted = true;
            
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    IsProcessing = true;
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
                IsProcessing = false;
                _updateStarted = false;
            });
        }
    }
}
