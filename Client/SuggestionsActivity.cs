using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    [Activity()]
    public class SuggestionsActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            var dlg = new Android.Support.V7.App.AlertDialog.Builder(this);
            if (Client.Server == null)
            {
                dlg.SetTitle("Connection to server is not established.");
                dlg.SetMessage("Please make sure you are connected to the server and try again.");
                dlg.SetNegativeButton("Exit", (sender, args) =>
                {
                    FinishAffinity();
                });
                dlg.Show();
                return;
            }
            Messenger.Default.AddListener(Messages.Shutdown, () =>
            {
                RunOnUiThread(() =>
                {
                    dlg = new Android.Support.V7.App.AlertDialog.Builder(this);
                    dlg.SetMessage("Disconnected from server.");
                    dlg.SetMessage("The server has shutdown. Please try again later.");
                    dlg.SetPositiveButton("EXIT", (sender, args) =>
                    {
                        FinishAffinity();
                    });
                    dlg.SetCancelable(false);
                    dlg.Show();
                });
            });
            
            base.OnCreate(savedInstanceState);

            // Create your application here
        }
    }
}