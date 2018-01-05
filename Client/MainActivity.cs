using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Widget;
using Android.OS;

namespace norsu.ass
{
    [Activity(Icon = "@drawable/ic_launcher",Label = "NORSU ASS", MainLauncher = true, Theme = "@style/Theme.Splash",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, NoHistory = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Task.Factory.StartNew(() =>
            {
                StartActivity(new Intent(Application.Context, typeof(LoginActivity)));
            }).ContinueWith(d =>
            {
                Finish();
            });
        }
    }
}

