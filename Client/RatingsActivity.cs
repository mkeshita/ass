using Android.App;
using Android.Content.PM;
using Android.OS;

namespace norsu.ass
{
    [Activity(Label = "RatingsActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class RatingsActivity : Activity
    {
        private long OfficeId = 0;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (savedInstanceState == null)
            {
                Title = Intent.GetStringExtra("name");
                OfficeId = Intent.GetLongExtra("officeId", 0);
            }
            else
            {
                Title = savedInstanceState.GetString("name");
                OfficeId = savedInstanceState.GetLong("officeId");
            }
            
            
            SetContentView(Resource.Layout.Ratings);
            
            // Create your application here
            ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;

            var ratingsTab = ActionBar.NewTab();
            ratingsTab.SetText("RATINGS");
            ratingsTab.SetIcon(Resource.Drawable.ic_action_star_half);
            ratingsTab.TabSelected += RatingsTabOnTabSelected;
            ActionBar.AddTab(ratingsTab);
            
            var tab = ActionBar.NewTab().SetText("SUGGESTIONS");
            tab.TabSelected += TabOnTabSelected;
            tab.SetIcon(Resource.Drawable.ic_action_comment);
            ActionBar.AddTab(tab);
        }
        
        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutLong("officeId",OfficeId);
            outState.PutString("name",Title);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
            OfficeId = savedInstanceState.GetLong("officeId");
            Title = savedInstanceState.GetString("name");
        }

        private SuggestionsFragment suggestions => new SuggestionsFragment(OfficeId);
        private void TabOnTabSelected(object sender, ActionBar.TabEventArgs e)
        {
            e.FragmentTransaction.Replace(Resource.Id.fragmentContainer, suggestions);
        }

        private RatingsFragment ratings => new RatingsFragment(OfficeId);
        
        private void RatingsTabOnTabSelected(object sender, ActionBar.TabEventArgs e)
        {
            e.FragmentTransaction.Replace(Resource.Id.fragmentContainer, ratings);
        }
    }
}