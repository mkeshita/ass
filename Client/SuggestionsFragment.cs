using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    class SuggestionsFragment : ListFragment
    {
        private ProgressBar _progress;
        
        public SuggestionsFragment(long officeId)
        {
            OfficeId = officeId;
        }
        
        public long OfficeId { get; set; }
        
        
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            var view = inflater.Inflate(Resource.Layout.SuggestionsTab, container, false);
            
            _progress = view.FindViewById<ProgressBar>(Resource.Id.progress);
            //_list = view.FindViewById<ListView>(Resource.Id.listView);
            
            SetAdapter();

            return view;
        }

        private async void SetAdapter()
        {
            var offices = await Client.GetSuggestions(OfficeId);

            var adapter = new SuggestionsAdapter(Activity, offices.Items);

            ListAdapter = adapter;
            _progress.Visibility = ViewStates.Gone;
            
        }

        private void ListOnItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var row = e.View.FindViewById<TextView>(Resource.Id.body);
            if (row.Visibility == ViewStates.Gone)
                row.Visibility = ViewStates.Visible;
            else
                row.Visibility = ViewStates.Gone;
        }
        
    }
}