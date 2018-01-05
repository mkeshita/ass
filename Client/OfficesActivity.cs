using System.Collections.Generic;
using Android.App;
using Android.OS;
using norsu.ass;
using norsu.ass.Network;

namespace norsu.ass
{
    [Activity(Label = "Username")]
    public class OfficesActivity : ListActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Offices);

            var offices = new List<Office>()
            {
                new Office() {ShortName = "asdf", Rating = 3.5f},
                new Office() {ShortName = "asdf sdfg", Rating = 4.5f},
                new Office() {ShortName = "dddfg sd", Rating = 2.5f},
            };
            
            
            var adapter = new OfficesAdapter(this, offices);

            ListAdapter = adapter;
        }
    }
}