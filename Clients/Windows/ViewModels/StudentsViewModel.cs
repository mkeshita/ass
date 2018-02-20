using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using norsu.ass.Models;
using norsu.ass.Network;
using Comment = norsu.ass.Models.Comment;
using Suggestion = norsu.ass.Models.Suggestion;

namespace norsu.ass.Server.ViewModels
{
    class StudentsViewModel : ViewModelBase
    {
        private StudentsViewModel()
        {
            User.Cache.CollectionChanged += (sender, args) =>
            {
                _students.Filter = FilterStudent;
                OnPropertyChanged(nameof(AnonymousCount));
            };

            Students.CurrentChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(OneStarCount));
                OnPropertyChanged(nameof(TwoStarCount));
                OnPropertyChanged(nameof(ThreeStarCount));
                OnPropertyChanged(nameof(FourStarCount));
                OnPropertyChanged(nameof(FiveStarCount));
                OnPropertyChanged(nameof(SuggestionsCount));
                OnPropertyChanged(nameof(CommentsCount));
                OnPropertyChanged(nameof(UpVotesCount));
                OnPropertyChanged(nameof(DownVotesCount));
            };
            
            Messenger.Default.AddListener<User>(Messages.UserSaved,async user =>
            {
                var res = await Client.SaveUser(new UserInfo()
                {
                    Id = user.Id,
                    Access =(int) (user.Access??0),
                    Description = user.Course,
                    Firstname = user.Firstname,
                    Lastname = user.Lastname,
                    StudentId = user.StudentId,
                    Username = user.Username
                });
                if (res?.Success ?? false)
                {
                    if(res.Id!=user.Id)
                    user.ChangeId(res.Id);
                }
                
            });
            
            Messenger.Default.AddListener<User>(Messages.NewUserBegin, user =>
            {
                user.Access = AccessLevels.Student;
            });

            Messenger.Default.AddListener(Messages.DatabaseRefreshed, () =>
            {
                _students = null;
                OnPropertyChanged("");
            });
        }

        private static StudentsViewModel _instance;
        public static StudentsViewModel Instance => _instance ?? (_instance = new StudentsViewModel());

        private ListCollectionView _students;

        public ListCollectionView Students
        {
            get
            {
                if (_students != null) return _students;
                _students = new ListCollectionView(Models.User.Cache);
                _students.Filter = FilterStudent;
                
                return _students;
            }
        }

        private ICommand _changePictureCommand;

        public ICommand ChangePictureCommand => _changePictureCommand ?? (_changePictureCommand = new DelegateCommand<User>(
        async d =>
        {
            var pic = ImageProcessor.GetPicture(256);
            if (pic == null) return;

            var result = await Client.SetPicture(d.Id, pic);
            if (result?.Success ?? false)
            {
                d.Update(nameof(User.Picture), pic);
            }
            else
            {
                MainViewModel.ShowToast("Error uploading picture.");
            }
        },d=>d!=null));

        private ICommand _resetPasswordCommand;

        public ICommand ResetPasswordCommand => _resetPasswordCommand ?? (_resetPasswordCommand = new DelegateCommand<User>(
        async d =>
        {
            var result = await Client.ResetPassword(d.Id);
            if (result?.Success ?? false)
            {
                
            }
            else
            {
                MainViewModel.ShowToast("Error resetting password");
            }
        },d=>d!=null));

        private ICommand _deleteCommand;

        public ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new DelegateCommand<User>(async d =>
        {
            var result = await Client.DeleteUser(d.Id);
            if (result?.Success ?? false)
            {
                d.Delete(false);
            }
            else
            {
                MainViewModel.ShowToast("Error deleting student");
            }
        }));

        private bool FilterStudent(object o)
        {
            if (!(o is Models.User s)) return false;
            return s.Access == AccessLevels.Student && !s.IsAnnonymous;
        }

        public long AnonymousCount => User.Cache.Count(x => x.IsAnnonymous);

        public long OneStarCount
        {
            get
            {
                if (!(Students.CurrentItem is User u)) return 0;
                return Rating.Cache.Count(x => x.UserId == u.Id && x.Value == 1);
            }
        }

        public long TwoStarCount
        {
            get
            {
                if (!(Students.CurrentItem is User u))
                    return 0;
                return Rating.Cache.Count(x => x.UserId == u.Id && x.Value == 2);
            }
        }

        public long ThreeStarCount
        {
            get
            {
                if (!(Students.CurrentItem is User u))
                    return 0;
                return Rating.Cache.Count(x => x.UserId == u.Id && x.Value == 3);
            }
        }

        public long FourStarCount
        {
            get
            {
                if (!(Students.CurrentItem is User u))
                    return 0;
                return Rating.Cache.Count(x => x.UserId == u.Id && x.Value == 4);
            }
        }

        public long FiveStarCount
        {
            get
            {
                if (!(Students.CurrentItem is User u))
                    return 0;
                return Rating.Cache.Count(x => x.UserId == u.Id && x.Value == 5);
            }
        }

        public long SuggestionsCount
        {
            get
            {
                if (!(Students.CurrentItem is User u))
                    return 0;
                return Suggestion.Cache.Count(x => x.Id == u.Id);
            }
        }

        public long CommentsCount
        {
            get
            {
                if (!(Students.CurrentItem is User u))
                    return 0;
                return Comment.Cache.Count(x => x.UserId == u.Id);
            }
        }

        public long UpVotesCount
        {
            get
            {
                if (!(Students.CurrentItem is User u))
                    return 0;
                return Like.Cache.Count(x => x.UserId == u.Id && !x.Dislike);
            }
        }

        public long DownVotesCount
        {
            get
            {
                if (!(Students.CurrentItem is User u))
                    return 0;
                return Like.Cache.Count(x => x.UserId == u.Id && x.Dislike);
            }
        }
    }
}
