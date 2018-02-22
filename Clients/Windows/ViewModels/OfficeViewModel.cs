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
                        office = Office.Cache.FirstOrDefault(x => x.Id == pic.OfficeId);
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
            
            //NetworkComms.AppendGlobalIncomingPacketHandler<Network.Suggestions>(Network.Suggestions.Header, HandleSuggestions);
            //NetworkComms.AppendGlobalIncomingPacketHandler<Network.OfficeRatings>(OfficeRatings.Header, HandleOfficeRatings);
            //NetworkComms.AppendGlobalIncomingPacketHandler<Network.Comments>(Comments.Header, HandlerComments);
            //NetworkComms.AppendGlobalIncomingPacketHandler<Network.Votes>(Votes.Header, VotesHandler);

            Models.Suggestion.Cache.CollectionChanged += (sender, args) =>
            {
                RatingsChanged();
            };

            
            Messenger.Default.AddListener(Messages.DatabaseRefreshed,async () =>
            {
                _offices = null;
                _officeAdmins = null;
                _ratings = null;
                _nextOfficeCommand = null;
                _previousOfficeCommand = null;
                
                await TaskEx.Delay(1111);
                
                OnPropertyChanged(nameof(Offices));
                OnPropertyChanged(nameof(OfficeAdmins));
                OnPropertyChanged(nameof(NextOfficeCommand));
                OnPropertyChanged("");
                RatingsChanged();
                
                CheckOfficeCount();
                
                Messenger.Default.Broadcast(Messages.OfficeViewModelRefreshed);
            });
        }

        private void VotesHandler(PacketHeader packetheader, Connection connection, Votes votes)
        {
            if (votes == null) return;
            DatabaseTasks.Enqueue(new Task(() =>
            {
                foreach (var vote in votes.List)
                {
                    var v = Like.GetById(vote.Id);
                    if (v == null)
                    {
                        v = new Like();
                        v.Id = vote.Id;
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
                    var com = Models.Comment.GetById(comment.Id);
                    if (com == null)
                    {
                        com = new Models.Comment();
                        com.Id = comment.Id;
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
                    var r = Rating.GetById(rating.Id);
                    if (r == null)
                    {
                        r = new Rating();
                        r.Defer = true;
                        r.Id = rating.Id;
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
                    var sug = Models.Suggestion.GetById(suggestion.Id);
                    if (sug == null)
                    {
                        sug = new Models.Suggestion();
                        sug.Defer = true;
                        sug.Id = suggestion.Id;
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
                    
                    Client.Send(new GetVotes(){SuggestionId = sug.Id});
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
                _offices = null;
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
                    
                        if (_offices.IsAddingNew)
                            return;
                        SelectedOfficeChanged?.Invoke((Models.Office) _offices.CurrentItem);
                    
                    //DownloadOffice((Office) _offices.CurrentItem);
                };
                
                return _offices;
            }
        }


        public static Action<Office> SelectedOfficeChanged { get; set; }

        // private void DownloadOffice(Office office)
        // {
        // if (office == null) return;
        // Client.Send(Packet.GET_SUGGESTIONS, office.Id);
        // Client.Send(Packet.GET_REVIEWS, office.Id);
        // }

        private bool Filter(object o)
        {
            
            if (LoginViewModel.Instance.User?.Access == AccessLevels.SuperAdmin) return true;
            var ofc = o as Office;
            if (ofc == null) return false;

            return OfficeAdmin.Cache.Any(x => x.OfficeId == ofc.Id && x.UserId == LoginViewModel.Instance.User?.Id);
            
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
