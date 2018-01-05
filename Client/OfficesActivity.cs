using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using norsu.ass;
using norsu.ass.Network;

namespace norsu.ass
{
    [Activity(Label = "Username")]
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
        }
    }
}