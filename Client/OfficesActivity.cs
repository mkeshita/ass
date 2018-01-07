using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using norsu.ass;
using norsu.ass.Network;
using AlertDialog = Android.App.AlertDialog;

namespace norsu.ass
{
    [Activity(Label = "Username", Theme = "@style/AppTheme.NoActionBar", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class OfficesActivity : Activity
    {
        private ListView _offices;
        private TextView _username,_fullname;
        private ImageView _picture;
        
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (string.IsNullOrEmpty(Client.Username))
            {
                StartActivity(new Intent(Application.Context,typeof(LoginActivity)));
                Finish();
            }
            
            SetContentView(Resource.Layout.Offices);

            _username = FindViewById<TextView>(Resource.Id.userName);
            _fullname = FindViewById<TextView>(Resource.Id.name);
            _picture = FindViewById<ImageView>(Resource.Id.picture);
            
            _offices = FindViewById<ListView>(Resource.Id.officesList);
            _offices.ItemClick += OfficesOnItemClick;
            
            _username.Text = Client.Username;
            _fullname.Text = Client.Fullname;
            var usr = Client.GetPicture(Client.UserId);
            if (usr != null)
                _picture.SetImageBitmap(BitmapFactory.DecodeByteArray(usr.Picture, 0, usr.Picture.Length));
            else
            {
                Messenger.Default.AddListener<UserPicture>(Messages.PictureReceived,
                    user =>
                    {
                        if (user.UserId != Client.UserId) return;
                        RunOnUiThread(() =>
                            _picture.SetImageBitmap(BitmapFactory.DecodeByteArray(user.Picture, 0, user.Picture.Length)));
                    });
            }

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

            _offices.Adapter = adapter;

            var progress = FindViewById<ProgressBar>(Resource.Id.progress);
            progress.Visibility = ViewStates.Gone;
        }

        private void OfficesOnItemClick(object o, AdapterView.ItemClickEventArgs e)
        {
            var office = ((OfficesAdapter) _offices.Adapter)[e.Position];

            Client.SelectedOffice = office;
            StartActivity(typeof(OfficeActivity));
        }
        
    }
}