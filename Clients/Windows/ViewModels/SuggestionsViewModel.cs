using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using norsu.ass.Network;

namespace norsu.ass.Server.ViewModels
{
    class SuggestionsViewModel : ViewModelBase
    {
        private SuggestionsViewModel()
        {
            OfficeViewModel.Instance.Offices.CurrentChanged += (sender, args) =>
            {
                Suggestions.Filter = FilterSuggestion;
            };
            
            Models.Suggestion.Cache.CollectionChanged += (sender, args) =>
            {
                _suggestions.Filter = FilterSuggestion;
            };
        }

        private bool FilterSuggestion(object o)
        {
            if (!(o is Models.Suggestion msg))
                return false;
            var selectedOffice = OfficeViewModel.Instance.Offices.CurrentItem as Models.Office;
            return msg.OfficeId == selectedOffice?.ServerId;
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
                _suggestions.CustomSort = new SuggestionSorter();
                
                _suggestions.CurrentChanged += (sender, args) =>
                {
                    if (_suggestions.CurrentItem == null)
                        return;
                    var sug = ((Models.Suggestion) _suggestions.CurrentItem);
                    var req = new GetCommentsDesktop();
                    var comments = Models.Comment.Cache.Where(x => x.SuggestionId == sug.ServerId).ToList();
                    if (comments.Count > 0)
                    {
                        req.HighestId = comments.OrderByDescending(x => x.ServerId).Select(x => x.ServerId)
                            .FirstOrDefault();
                    }
                    req.SuggestionId = sug.ServerId;
                    Client.Send(req);
                };
                return _suggestions;
            }
        }

        class SuggestionSorter : IComparer, IComparer<Models.Suggestion>
        {
            public int Compare(object x, object y)
            {
                return Compare(x as Models.Suggestion, y as Models.Suggestion);
            }

            public int Compare(Models.Suggestion x, Models.Suggestion y)
            {
                return x.Time.CompareTo(y.Time);
            }
        }

        private static SuggestionsViewModel _instance;
        public static SuggestionsViewModel Instance => _instance ?? (_instance = new SuggestionsViewModel());

        private ICommand _toggleCommentsCommand;
        
        public ICommand ToggleCommentsCommand =>
            _toggleCommentsCommand ?? (_toggleCommentsCommand = new DelegateCommand<Models.Suggestion>(
                async d =>
                {
                    EnableToggleComments = false;
                    var res = await Client.ToggleComments(d.Id, LoginViewModel.Instance.User.Id);
                    EnableToggleComments = true;
                    
                    if (res?.Success ?? false)
                    {
                        if(d.AllowComments)
                            MainViewModel.ShowToast("Unable to disable comments. Please make sure you are connected to server.");
                        d.Update(nameof(d.AllowComments),res.AllowComments);
                    }
                    OnPropertyChanged(nameof(ToggleCommentsCommand));
                }));
        
        private bool _ProcessingToggleComments = true;

        public bool EnableToggleComments
        {
            get => _ProcessingToggleComments;
            set
            {
                if(value == _ProcessingToggleComments)
                    return;
                _ProcessingToggleComments = value;
                OnPropertyChanged(nameof(EnableToggleComments));
            }
        }

        
    }
}
