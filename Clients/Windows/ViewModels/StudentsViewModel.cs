using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using norsu.ass.Models;

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

        private bool FilterStudent(object o)
        {
            if (!(o is Models.User s)) return false;
            return s.Access == AccessLevels.Student && !s.IsAnnonymous;
        }

        public long AnonymousCount => User.Cache.Count(x => x.IsAnnonymous);
    }
}
