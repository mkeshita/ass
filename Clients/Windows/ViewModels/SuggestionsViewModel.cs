using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace norsu.ass.Server.ViewModels
{
    class SuggestionsViewModel : ViewModelBase
    {
        private SuggestionsViewModel()
        {
            
        }

        private static SuggestionsViewModel _instance;
        public static SuggestionsViewModel Instance => _instance ?? (_instance = new SuggestionsViewModel());
        
        
    }
}
