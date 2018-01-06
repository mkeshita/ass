using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using norsu.ass;
using norsu.ass.Network;

namespace norsu.ass
{
    [Activity(Label = "Username", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class OfficesActivity : ListActivity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (string.IsNullOrEmpty(Client.Username))
            {
                StartActivity(new Intent(Application.Context,typeof(LoginActivity)));
                Finish();
            }
            
            SetContentView(Resource.Layout.Offices);

            Title = Client.Username;
            
            var offices = await Client.GetOffices();
            var dlg = new AlertDialog.Builder(this);
            dlg.SetTitle("Connection to server is not established.");
            dlg.SetMessage("Please make sure you are connected to the server and try again.");
            dlg.SetNegativeButton("Exit", (sender, args) =>
            {
                Finish();
            });
            while (offices == null)
            {
                
                offices = await Client.GetOffices();
            }
            var adapter = new OfficesAdapter(this, offices.Items);

            ListAdapter = adapter;

            var progress = FindViewById<ProgressBar>(Resource.Id.progress);
            progress.Visibility = ViewStates.Gone;
        }

        protected override void OnListItemClick(ListView l, View v, int position, long id)
        {
            base.OnListItemClick(l, v, position, id);
            var office = ((OfficesAdapter) ListAdapter)[position];
            
            var options = new Bundle();
            options.PutString("name", office.ShortName);
            options.PutLong("id",office.Id);
            var intent = new Intent(Application.Context, typeof(RatingsActivity));
            intent.PutExtra("name", office.ShortName);
            intent.PutExtra("id", office.Id);
            StartActivity(intent);
            
        }
    }
}