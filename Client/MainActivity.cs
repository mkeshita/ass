using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Widget;
using Android.OS;
using norsu.ass.Network;

namespace norsu.ass
{
    [Activity(Icon = "@drawable/ic_launcher",Label = "NORSU ASS",  Theme = "@style/Theme.Splash", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, NoHistory = true)]
    public class MainActivity : Activity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

                await Client.FindServer();
               // StartActivity(new Intent(Application.Context, typeof(LoginActivity)));
                StartActivity(typeof(LoginActivity));
                Finish();
        }
    }
}

