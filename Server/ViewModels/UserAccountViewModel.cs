namespace norsu.ass.Server.ViewModels
{
    class UserAccountViewModel : ViewModelBase
    {
        private UserAccountViewModel() { }
        private static UserAccountViewModel _instance;
        public static UserAccountViewModel Instance => _instance ?? (_instance = new UserAccountViewModel());
        
        
    }
}
