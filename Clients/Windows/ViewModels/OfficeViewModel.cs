using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using norsu.ass.Models;
using norsu.ass.Network;
using Office = norsu.ass.Models.Office;

namespace norsu.ass.Server.ViewModels
{
    class OfficeViewModel : ViewModelBase
    {
        private OfficeViewModel()
        {
            Messenger.Default.AddListener(Messages.LoggedIn, () =>
            {
                Offices.Filter = Filter;
            });
            
            Messenger.Default.AddListener<OfficePicture>(Messages.OfficePictureReceived, pic =>
            {
                var office = Office.Cache.FirstOrDefault(x => x.Id == pic.OfficeId);
                if (office == null) return;
                office.Update(nameof(Office.Picture), pic.Picture);
            });
        }

        private static OfficeViewModel _instance;
        public static OfficeViewModel Instance => _instance ?? (_instance = new OfficeViewModel());

        private bool _IsDialogOpen;

        public bool IsDialogOpen
        {
            get => _IsDialogOpen;
            set
            {
                if (value == _IsDialogOpen)
                    return;
                _IsDialogOpen = value;
                OnPropertyChanged(nameof(IsDialogOpen));
            }
        }

        private int _DialogIndex = 0;

        public int DialogIndex
        {
            get => _DialogIndex;
            set
            {
                if (value == _DialogIndex)
                    return;
                _DialogIndex = value;
                OnPropertyChanged(nameof(DialogIndex));
            }
        }

        public async void DownloadData()
        {
            CheckOfficeCount();
            return;
            
            var res = await Client.GetOffices();
            if (res == null) return;
            foreach (var item in res.Items)
            {
                var office = Office.Cache.FirstOrDefault(x => x.Id == item.Id);
                if (office == null)
                {
                    office = new Office();
                    office.Defer = true;
                    office.Save();
                    office.ChangeId(item.Id);
                }
                office.LongName = item.LongName;
                office.ShortName = item.ShortName;
                office.Save();
            }
            Client.Instance.FetchOfficePictures(res.Items.Select(x => x.Id).ToList());
            CheckOfficeCount();
        }

        private ICommand _previousOfficeCommand;

        public ICommand PreviousOfficeCommand =>
            _previousOfficeCommand ?? (_previousOfficeCommand = new DelegateCommand(
                d =>
                {
                    Offices.MoveCurrentToPrevious();
                }, d =>
                {
                    if (Offices.Count == 0) return false;
                    if(Offices.CurrentPosition == 0)
                        return false;
                    return true;
                }));

        private ICommand _nextOfficeCommand;

        public ICommand NextOfficeCommand => _nextOfficeCommand ?? (_nextOfficeCommand = new DelegateCommand(
        d =>
        {
            Offices.MoveCurrentToNext();
        }, d =>
        {
            if (Offices.Count == 0)
                return false;
            if (Offices.CurrentPosition+1 == Offices.Count)
                return false;
            return true;
        }));

        private void CheckOfficeCount()
        {
            if (Offices.Count == 0)
            {
                DialogIndex = 0;
                IsDialogOpen = true;
            }
            else
            {
                if (DialogIndex == 0)
                    IsDialogOpen = false;
            }

            if(Offices.CurrentItem == null)
                Offices.MoveCurrentToFirst();
        }

        private ListCollectionView _offices;

        public ListCollectionView Offices
        {
            get
            {
                if (_offices != null) return _offices;
                _offices = new ListCollectionView(Office.Cache);
                Office.Cache.CollectionChanged += (sender, args) =>
                {
                    _offices.Filter = Filter;
                    CheckOfficeCount();
                };
                _offices.Filter = Filter;
                return _offices;
            }
        }

        private bool Filter(object o)
        {
            return true;
            
            
            if (LoginViewModel.Instance.User?.Access == AccessLevels.SuperAdmin) return true;
            var ofc = o as Office;
            if (ofc == null) return false;

            return OfficeAdmin.Cache.Any(x => x.OfficeId == ofc.Id && x.UserId == LoginViewModel.Instance.User.Id);
            
        }

        private void RatingsChanged()
        {
            Ratings.Filter = FilterRating;
            OnPropertyChanged(nameof(OneStar));
            OnPropertyChanged(nameof(TwoStars));
            OnPropertyChanged(nameof(ThreeStars));
            OnPropertyChanged(nameof(FourStars));
            OnPropertyChanged(nameof(FiveStars));
            OnPropertyChanged(nameof(LatestRating));
            OnPropertyChanged(nameof(LatestSuggestion));
            OnPropertyChanged(nameof(TopSuggestion));
            OfficeAdmins.Filter = FilterOfficeAdmins;
        }

        private ListCollectionView _suggestions;

        public ListCollectionView Suggestions
        {
            get
            {
                if (_suggestions != null)
                    return _suggestions;
                _suggestions = new ListCollectionView(Models.Suggestion.Cache);
                _suggestions.Filter = FilterSuggestion;
                Models.Suggestion.Cache.CollectionChanged += (sender, args) =>
                {
                    _suggestions.Filter = FilterSuggestion;
                };
                return _suggestions;
            }
        }

        private bool FilterSuggestion(object o)
        {
            if (!(o is Models.Suggestion msg))
                return false;
            var selectedOffice = Offices.CurrentItem as Office;
            return msg.OfficeId == selectedOffice?.Id;
        }

        private ListCollectionView _ratings;

        public ListCollectionView Ratings
        {
            get
            {
                if (_ratings != null)
                    return _ratings;
                _ratings = new ListCollectionView(Rating.Cache);
                _ratings.Filter = FilterRating;
                Office.Cache.CollectionChanged += (sender, args) =>
                {
                    _ratings.Filter = FilterRating;
                };
                return _ratings;
            }
        }

        private bool FilterRating(object o)
        {
            if (!(o is Rating rating))
                return false;
            return rating.OfficeId == (Offices.CurrentItem as Office)?.Id;
        }

        private bool FilterOfficeAdmins(object o)
        {
            if (Offices.CurrentItem == null)
                return false;
            var adm = o as OfficeAdmin;
            return adm?.OfficeId == ((Office) Offices.CurrentItem).Id;
        }

        private ListCollectionView _officeAdmins;

        public ListCollectionView OfficeAdmins
        {
            get
            {
                if (_officeAdmins != null)
                    return _officeAdmins;
                _officeAdmins = new ListCollectionView(OfficeAdmin.Cache);
                OfficeAdmin.Cache.CollectionChanged += (sender, args) =>
                {
                    _officeAdmins.Filter = FilterOfficeAdmins;
                };
                _officeAdmins.Filter = FilterOfficeAdmins;
                return _officeAdmins;
            }
        }

        public Rating LatestRating => Rating.Cache
            .OrderByDescending(x => x.Time)
            .FirstOrDefault(x => x.OfficeId == ((Office) Offices?.CurrentItem)?.Id);

        public Models.Suggestion LatestSuggestion => Models.Suggestion.Cache
            .OrderByDescending(x => x.Time)
            .FirstOrDefault(x => x.OfficeId == ((Office) Offices?.CurrentItem)?.Id);

        public Models.Suggestion TopSuggestion => Models.Suggestion.Cache
            .OrderByDescending(x => x.Votes)
            .FirstOrDefault(x => x.OfficeId == ((Office) Offices?.CurrentItem)?.Id);
        
        public long OneStar =>
            Rating.Cache.Count(d => d.Value == 1 && d.OfficeId == ((Office) Offices?.CurrentItem)?.Id);

        public long TwoStars =>
            Rating.Cache.Count(d => d.Value == 2 && d.OfficeId == ((Office) Offices?.CurrentItem)?.Id);

        public long ThreeStars =>
            Rating.Cache.Count(d => d.Value == 3 && d.OfficeId == ((Office) Offices?.CurrentItem)?.Id);

        public long FourStars =>
            Rating.Cache.Count(d => d.Value == 4 && d.OfficeId == ((Office) Offices?.CurrentItem)?.Id);

        public long FiveStars =>
            Rating.Cache.Count(d => d.Value == 5 && d.OfficeId == ((Office) Offices?.CurrentItem)?.Id);
    }
}
