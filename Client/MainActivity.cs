using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Widget;
using Android.OS;
using Android.Views;
using norsu.ass.Network;

namespace norsu.ass
{
    [Activity(Icon = "@drawable/ic_launcher",Label = "NORSU ASS",  Theme = "@style/Theme.Splash", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, NoHistory = true)]
    public class MainActivity : Activity
    {
        private ProgressBar _progress;
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
    
            SetContentView(Resource.Layout.Main);

            _progress = FindViewById<ProgressBar>(Resource.Id.progress);
        }

        protected override void OnStart()
        {
            base.OnStart();
            FindServer();
        }

        private async void FindServer()
        {
            _progress.Visibility = ViewStates.Visible;
            await Client.FindServer();
            _progress.Visibility = ViewStates.Gone;
            if (Client.Server == null)
            {
                SetTheme(Android.Resource.Style.ThemeHoloLightDialogNoActionBar);
                var dlg = new AlertDialog.Builder(this);
                dlg.SetTitle("Retry to connect to server?");
                dlg.SetCancelable(false);
                dlg.SetMessage("The server is not accessible. Make sure you are connected to NORSU's wifi and try again.");
                dlg.SetPositiveButton("RETRY", (sender, args) =>
                {
                    FindServer();
                });
                dlg.SetNegativeButton("EXIT", (sender, args) =>
                {
                    Finish();
                });
                dlg.Create().Show();
            }
            else
            {
                StartActivity(typeof(LoginActivity));
                Finish();
            }
        }
    }
}

