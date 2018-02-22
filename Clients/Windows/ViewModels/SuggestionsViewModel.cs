using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using norsu.ass.Models;
using norsu.ass.Network;
using Xceed.Words.NET;
using Suggestion = norsu.ass.Models.Suggestion;

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
            
            Messenger.Default.AddListener<Suggestion>(Messages.ModelSelected, s =>
            {
                OnPropertyChanged(nameof(CanDeleteSuggestions));
            });
            
            Messenger.Default.AddListener(Messages.LoggedIn, () =>
            {
                OnPropertyChanged(nameof(CanDeleteSuggestions));
            });

            Messenger.Default.AddListener(Messages.OfficeViewModelRefreshed, () =>
            {
                _comments = null;
                _suggestions = null;

                OnPropertyChanged(nameof(Suggestions));
                OnPropertyChanged(nameof(Comments));
            });
        }

        private bool FilterSuggestion(object o)
        {
            if (!(o is Models.Suggestion msg))
                return false;
            var selectedOffice = OfficeViewModel.Instance.Offices.CurrentItem as Models.Office;
            return msg.OfficeId == selectedOffice?.Id;
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
                OfficeViewModel.SelectedOfficeChanged = office =>
                {
                    _suggestions.Refresh();
                    SelectedOffice = office;
                };
                _suggestions.CustomSort = new SuggestionSorter();
               
                return _suggestions;
            }
        }
        
        private ListCollectionView _comments;

        public ListCollectionView Comments
        {
            get
            {
                if (_comments != null) return _comments;
                _comments = new ListCollectionView(Models.Comment.Cache);
                Suggestions.CurrentChanged += (sender, args) =>
                {
                    _comments.Filter = FilterComment;
                };
                _comments.Filter = FilterComment;
                return _comments;
            }
        }

        private bool FilterComment(object o)
        {
            if (!(Suggestions.CurrentItem is Models.Suggestion s)) return false;
            if (!(o is Models.Comment c)) return false;
            return c.SuggestionId == s.Id;
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
        //public static SuggestionsViewModel Instance => _instance ?? (_instance = new SuggestionsViewModel());
        public static SuggestionsViewModel GetInstance()
        {
            return _instance ?? (_instance = new SuggestionsViewModel());
        }
        
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
                        d.Update(nameof(d.AllowComments), res.AllowComments);
                    }
                    else
                    {
                        if (d.AllowComments)
                            MainViewModel.ShowToast("Unable to disable comments. Please make sure you are connected to server.");
                        else
                            MainViewModel.ShowToast("Unable to enable comments. Please make sure you are connected to server.");
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

        private string _ReplyText;

        public string ReplyText
        {
            get => _ReplyText;
            set
            {
                if(value == _ReplyText)
                    return;
                _ReplyText = value;
                OnPropertyChanged(nameof(ReplyText));
                OnPropertyChanged(nameof(CanSendComment));
            }
        }

        private bool _CanSendComment = true;

        public bool CanSendComment
        {
            get => _CanSendComment && !string.IsNullOrEmpty(ReplyText);
            set
            {
                if(value == _CanSendComment)
                    return;
                _CanSendComment = value;
                OnPropertyChanged(nameof(CanSendComment));
            }
        }

        private ICommand _replyCommand;

        public ICommand ReplyCommand => _replyCommand ?? (_replyCommand = new DelegateCommand(async d =>
        {
            var comment = new Models.Comment()
            {
                Defer = true,
                Message = ReplyText,
                UserId = LoginViewModel.Instance.User.Id,
                SuggestionId = ((Models.Suggestion) Suggestions.CurrentItem).Id,
            };
            
            CanSendComment = false;
            var res = await Client.SendComment(
                comment.SuggestionId,
                comment.UserId,
                ReplyText);
            CanSendComment = true;
            
            if (res?.Success ?? false)
            {
                comment.Id = res.CommentId;
                comment.Save();
                ReplyText = "";
            }
            else
            {
                MainViewModel.ShowToast("Sending comment failed!");
            }
            
        }));

        private bool _CanDeleteSuggestions = true;

        public bool CanDeleteSuggestions
        {
            get => _CanDeleteSuggestions && ((Client.Server?.CanDeleteSuggestion??false) || LoginViewModel.Instance.User?.Access == AccessLevels.SuperAdmin);
            set
            {
                if(value == _CanDeleteSuggestions)
                    return;
                _CanDeleteSuggestions = value;
                OnPropertyChanged(nameof(CanDeleteSuggestions));
            }
        }

        public List<Models.Suggestion> GetSuggestions()
        {
            if(Suggestions.Cast<Models.Suggestion>().All(x=>!x.IsSelected))
                return new List<Suggestion>();
            return Suggestions.Cast<Models.Suggestion>()
                .Where(x => x.IsSelected).ToList();
        }

        private ICommand _DeleteSelectedSuggestionsCommand;

        public ICommand DeleteSelectedSuggestionsCommand =>
            _DeleteSelectedSuggestionsCommand ?? (_DeleteSelectedSuggestionsCommand = new DelegateCommand(
                async d =>
                {
                    if (GetSuggestions().Count == 0) return;
                    CanDeleteSuggestions = false;
                    
                    var res = await Client.DeleteSuggestions(LoginViewModel.Instance.User.Id,
                        GetSuggestions().Select(x=>x.Id).ToList());
                    CanDeleteSuggestions = true;

                    if (res?.Success ?? false)
                    {
                        Models.Suggestion.Delete(GetSuggestions().Select(x => x.Id).ToList());
                    }
                    else
                    {
                        MainViewModel.ShowToast("Failed to delete suggestions.");
                    }
                },d=>CanDeleteSuggestions && GetSuggestions().Count>0));

        internal static void Print(string path)
        {
            var info = new ProcessStartInfo(path);
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.UseShellExecute = true;
            info.Verb = "PrintTo";
            try
            {
                Process.Start(info);
            }
            catch (Exception e)
            {
                //
            }
        }

        private Models.Office _SelectedOffice;

        public Models.Office SelectedOffice
        {
            get => _SelectedOffice;
            set
            {
                if(value == _SelectedOffice)
                    return;
                _SelectedOffice = value;
                OnPropertyChanged(nameof(SelectedOffice));
            }
        }

        

        private ICommand _printSuggestionsCommand;

        public ICommand PrintSuggestionsCommand =>
            _printSuggestionsCommand ?? (_printSuggestionsCommand = new DelegateCommand(
            d =>
            {
                if (!(OfficeViewModel.Instance.Offices.CurrentItem is Models.Office office)) return;
                
                if(!Directory.Exists("Temp"))
                    Directory.CreateDirectory("Temp");

                var temp = Path.Combine("Temp", $"Suggestions [{DateTime.Now:yy-MMM-dd}].docx");
                var template = $@"Templates\Suggestions.docx";

                var fontSize = 0d;
                var borders = new List<double>();
                using(var doc = File.Exists(template) ? DocX.Load(template) : DocX.Create(temp))
                {
                    var tbl = doc.Tables.FirstOrDefault();
                    if (tbl == null)
                    {
                        tbl = doc.AddTable(1, 7);
                        
                        var r = tbl.Rows[0];
                        var p = r.Cells[0].Paragraphs.First().Append("#");
                        p.Alignment = Alignment.center;

                        r.Cells[1].Paragraphs.First().Append("STUDENT").Alignment = Alignment.center;
                        r.Cells[2].Paragraphs.First().Append("SUBJECT").Alignment = Alignment.center;
                        r.Cells[3].Paragraphs.First().Append("BODY").Alignment = Alignment.center;
                        r.Cells[4].Paragraphs.First().Append("DATE SUBMITTED").Alignment = Alignment.center;
                        r.Cells[5].Paragraphs.First().Append("VOTES").Alignment = Alignment.center;
                        r.Cells[6].Paragraphs.First().Append("COMMENTS").Alignment = Alignment.center;

                        doc.InsertTable(tbl);
                    }

                    
                    for (int i = 0; i < tbl.ColumnCount; i++)
                    {
                        borders.Add(tbl.Rows[0].Cells[i].Width);
                    }
                    
                    var number = 0;
                    
                    foreach(Models.Suggestion item in Suggestions)
                    {
                        var r = tbl.InsertRow();
                        number++;
                        var p = r.Cells[0].Paragraphs.First().Append($"{number}");
                        p.LineSpacingAfter = 0;
                        p.Alignment = Alignment.center;

                      
                        p = r.Cells[1].Paragraphs.First().Append(item.User.Fullname);
                        p.LineSpacingAfter = 0;
                        p.Alignment = Alignment.left;

                        p = r.Cells[2].Paragraphs.First().Append(item.Title);
                        p.LineSpacingAfter = 0;
                        p.Alignment = Alignment.left;

                        p = r.Cells[3].Paragraphs.First().Append(item.Body);
                        p.LineSpacingAfter = 0;
                        p.Alignment = Alignment.left;
                        
                        p = r.Cells[4].Paragraphs.First().Append(item.Time.ToString("d"));
                        p.LineSpacingAfter = 0;
                        p.Alignment = Alignment.center;
                        
                        p = r.Cells[5].Paragraphs.First().Append(item.Votes.ToString());
                        p.LineSpacingAfter = 0;
                        p.Alignment = Alignment.center;
                        
                        p = r.Cells[6].Paragraphs.First().Append(item.Comments.Count.ToString());
                        p.LineSpacingAfter = 0;
                        p.Alignment = Alignment.center;
                        
                        for (var i = 0; i < borders.Count; i++)
                        {
                            r.Cells[i].Width = borders[i];
                            r.Cells[i].VerticalAlignment = VerticalAlignment.Center;
                            r.Cells[i].MarginBottom = 4;
                            r.Cells[i].MarginTop = 2;
                            r.Cells[i].MarginLeft = 7;
                            r.Cells[i].MarginRight = 7;
                            r.Cells[i].Paragraphs.First().FontSize(10);
                        }
                    }

                    doc.ReplaceText("[OFFICE]", office.LongName);

                    var border = new Xceed.Words.NET.Border(BorderStyle.Tcbs_single, BorderSize.one, 0,
                        System.Drawing.Color.Black);
                    tbl.SetBorder(TableBorderType.Bottom, border);
                    tbl.SetBorder(TableBorderType.Left, border);
                    tbl.SetBorder(TableBorderType.Right, border);
                    tbl.SetBorder(TableBorderType.Top, border);
                    tbl.SetBorder(TableBorderType.InsideV, border);
                    tbl.SetBorder(TableBorderType.InsideH, border);
                    
                    try
                    {
                        File.Delete(temp);
                    }
                    catch (Exception e)
                    {
                        //
                    }
                    
                    doc.SaveAs(temp);
                }
                
                Print(temp);
            }));

    }
}
