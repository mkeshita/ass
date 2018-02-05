using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using norsu.ass.Models;
using norsu.ass.Network;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using Office = norsu.ass.Models.Office;
using Packet = norsu.ass.Network.Packet;
using Suggestion = norsu.ass.Models.Suggestion;

namespace norsu.ass.Server.ViewModels
{
    class OfficeViewModel : ViewModelBase
    {
        private OfficeViewModel()
        {
            Messenger.Default.AddListener(Messages.LoggedIn, () =>
            {
                Offices.Filter = Filter;
                Offices.MoveCurrentToFirst();
                CheckOfficeCount();
                OnPropertyChanged(nameof(CurrentUser));
            });
            
            Messenger.Default.AddListener<OfficePicture>(Messages.OfficePictureReceived, pic =>
            {
                DatabaseTasks.Enqueue(new Task(async () =>
                {
                    
                
                Office office = null;
                while(true)
                    try
                    {
                        office = Office.Cache.FirstOrDefault(x => x.ServerId == pic.OfficeId);
                        break;
                    }
                    catch (Exception e)
                    {
                        await TaskEx.Delay(10);
                    }
                    
                if (office == null) return;
                office.Update(nameof(Office.Picture), pic.Picture);

                }));
                
                StartDatabaseTask();
            });
            
            NetworkComms.AppendGlobalIncomingPacketHandler<Network.Suggestions>(Network.Suggestions.Header, HandleSuggestions);
            NetworkComms.AppendGlobalIncomingPacketHandler<Network.OfficeRatings>(OfficeRatings.Header, HandleOfficeRatings);
            NetworkComms.AppendGlobalIncomingPacketHandler<Network.Comments>(Comments.Header, HandlerComments);
            NetworkComms.AppendGlobalIncomingPacketHandler<Network.Votes>(Votes.Header, VotesHandler);

            Models.Suggestion.Cache.CollectionChanged += (sender, args) =>
            {
                RatingsChanged();
            };
        }

        private void VotesHandler(PacketHeader packetheader, Connection connection, Votes votes)
        {
            if (votes == null) return;
            DatabaseTasks.Enqueue(new Task(() =>
            {
                foreach (var vote in votes.List)
                {
                    var v = Like.GetByServerId(vote.Id);
                    if (v == null)
                    {
                        v = new Like();
                        v.ServerId = vote.Id;
                        v.Defer = true;
                    }
                    v.Dislike = vote.DownVote;
                    v.SuggestionId = vote.SuggestionId;
                    v.UserId = vote.UserId;
                    v.Save();
                    v.Defer = false;
                }
            }));
            StartDatabaseTask();
        }

        private Queue<Task> DatabaseTasks = new Queue<Task>();
        private Task DatabaseTask;
        private void StartDatabaseTask()
        {
            if (DatabaseTask !=null && !DatabaseTask.IsCompleted) return;
            DatabaseTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (DatabaseTasks.Count == 0)
                    {
                        TaskEx.Delay(1000);
                        if (DatabaseTasks.Count == 0)
                        {
                            DatabaseTask = null;
                            return;
                        }
                    }
                    var task = DatabaseTasks.Dequeue();
                    try
                    {
                        task?.Start();
                    }
                    catch (Exception e)
                    {
                        //
                    }
                    
                    task?.Wait();
                }
            });
        }
        private void HandlerComments(PacketHeader packetheader, Connection connection, Comments c)
        {
            if(c == null) return;
            
            DatabaseTasks.Enqueue(new Task(() =>
            {
                foreach (var comment in c.Items)
                {
                    var com = Models.Comment.GetByServerId(comment.Id);
                    if (com == null)
                    {
                        com = new Models.Comment();
                        com.ServerId = comment.Id;
                        com.Defer = true;
                    }
                    com.Message = comment.Message;
                    com.ParentId = comment.ParentId;
                    com.SuggestionId = comment.SuggestionId;
                    com.Time = comment.Time;
                    com.UserId = comment.UserId;
                    com.Save();
                    com.Defer = false;
                }
            }));
            
            StartDatabaseTask();
        }
        
        private void HandleOfficeRatings(PacketHeader packetHeader, Connection connection, OfficeRatings rate)
        {
            if (rate == null) return;
            
           DatabaseTasks.Enqueue(new Task(() =>
            {
              
                foreach (var rating in rate.Ratings)
                {
                    var r = Rating.GetByServerId(rating.Id);
                    if (r == null)
                    {
                        r = new Rating();
                        r.Defer = true;
                        r.ServerId = rating.Id;
                    }
                    r.IsPrivate = rating.IsPrivate;
                    r.Message = rating.Message;
                    r.OfficeId = rating.OfficeId;
                    r.UserId = rating.UserId;
                    r.Value = rating.Rating;
                    r.Save();
                    r.Defer = false;
                }
            }));
            
            StartDatabaseTask();
        }
        
        private void HandleSuggestions(PacketHeader packetHeader, Connection connection, Suggestions s)
        {
            if (s == null) return;
            DatabaseTasks.Enqueue(new Task(() =>
            {
                foreach (var suggestion in s.Items)
                {
                    var sug = Models.Suggestion.GetByServerId(suggestion.Id);
                    if (sug == null)
                    {
                        sug = new Models.Suggestion();
                        sug.Defer = true;
                        sug.ServerId = suggestion.Id;
                    }

                    sug.AllowComments = suggestion.AllowComment;
                    sug.Body = suggestion.Body;
                    sug.CommentsDisabledBy = suggestion.CommentsDisabledBy;
                    sug.IsPrivate = suggestion.IsPrivate;
                    sug.OfficeId = suggestion.OfficeId;
                    sug.Time = suggestion.Time;
                    sug.Title = suggestion.Title;
                    sug.UserId = suggestion.UserId;
                    sug.Save();
                    sug.Defer = false;
                    
                    Client.Send(new GetVotes(){SuggestionId = sug.ServerId});
                }

            }));
            
            StartDatabaseTask();
        }

        public User CurrentUser => LoginViewModel.Instance.User;

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
            
            var res = await Client.GetOffices();
            if (res == null) return;
            
            DatabaseTasks.Enqueue(new Task(() =>
            {   
                foreach (var item in res.Items)
                {
                    var office = Office.GetByServerId(item.Id);
                    if (office == null)
                    {
                        office = new Office();
                        office.Defer = true;
                        office.ServerId = item.Id;
                    }
                    office.LongName = item.LongName;
                    office.ShortName = item.ShortName;
                    office.Save();
                    office.Defer = false;
                }
            }));
            StartDatabaseTask();
            
            Client.Instance.FetchOfficePictures(res.Items.Select(x => x.Id).ToList());
            OnPropertyChanged(nameof(Offices));
            CheckOfficeCount();
        }

        private ICommand _previousOfficeCommand;

        public ICommand PreviousOfficeCommand =>
            _previousOfficeCommand ?? (_previousOfficeCommand = new DelegateCommand(
                d =>
                {
                    if (Offices.CurrentPosition == 0)
                        Offices.MoveCurrentToLast();
                    else
                        Offices.MoveCurrentToPrevious();
                }, d => Offices.Count > 0));

        private ICommand _nextOfficeCommand;

        public ICommand NextOfficeCommand => _nextOfficeCommand ?? (_nextOfficeCommand = new DelegateCommand(
        d =>
        {
            if (Offices.CurrentPosition + 1 == Offices.Count)
                Offices.MoveCurrentToFirst();
            else
                Offices.MoveCurrentToNext();
        }, d => Offices.Count > 0));

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

            if (Offices.CurrentItem == null)
            {
                Offices.MoveCurrentToFirst();
            }
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
                    RatingsChanged();
                };
                
                _offices.Filter = Filter;

                _offices.CurrentChanged += (sender, args) =>
                {
                    RatingsChanged();
                    DownloadOffice((Office) _offices.CurrentItem);
                };
                
                return _offices;
            }
        }

        private void DownloadOffice(Office office)
        {
            if (office == null) return;
            Client.Send(Packet.GET_SUGGESTIONS, office.ServerId);
            Client.Send(Packet.GET_REVIEWS, office.ServerId);
        }

        private bool Filter(object o)
        {
            
            if (LoginViewModel.Instance.User?.Access == AccessLevels.SuperAdmin) return true;
            var ofc = o as Office;
            if (ofc == null) return false;

            return OfficeAdmin.Cache.Any(x => x.OfficeId == ofc.ServerId && x.UserId == LoginViewModel.Instance.User.Id);
            
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
                Rating.Cache.CollectionChanged += (sender, args) =>
                {
                    RatingsChanged();
                };
                return _ratings;
            }
        }

        private bool FilterRating(object o)
        {
            if (!(o is Rating rating))
                return false;
            return rating.OfficeId == (Offices.CurrentItem as Office)?.ServerId;
        }

        private bool FilterOfficeAdmins(object o)
        {
            if (Offices.CurrentItem == null)
                return false;
            var adm = o as OfficeAdmin;
            return adm?.OfficeId == ((Office) Offices.CurrentItem).ServerId;
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
            .FirstOrDefault(x => x.OfficeId == ((Office) Offices?.CurrentItem)?.ServerId);

        public Models.Suggestion LatestSuggestion => Models.Suggestion.Cache
            .OrderByDescending(x => x.Time)
            .FirstOrDefault(x => x.OfficeId == ((Office) Offices?.CurrentItem)?.ServerId);

        public Models.Suggestion TopSuggestion => Models.Suggestion.Cache
            .OrderByDescending(x => x.Votes)
            .FirstOrDefault(x => x.OfficeId == ((Office) Offices?.CurrentItem)?.ServerId);
        
        public long OneStar =>
            Rating.Cache.Count(d => d.Value == 1 && d.OfficeId == ((Office) Offices?.CurrentItem)?.ServerId);

        public long TwoStars =>
            Rating.Cache.Count(d => d.Value == 2 && d.OfficeId == ((Office) Offices?.CurrentItem)?.ServerId);

        public long ThreeStars =>
            Rating.Cache.Count(d => d.Value == 3 && d.OfficeId == ((Office) Offices?.CurrentItem)?.ServerId);

        public long FourStars =>
            Rating.Cache.Count(d => d.Value == 4 && d.OfficeId == ((Office) Offices?.CurrentItem)?.ServerId);

        public long FiveStars =>
            Rating.Cache.Count(d => d.Value == 5 && d.OfficeId == ((Office) Offices?.CurrentItem)?.ServerId);
    }
}
