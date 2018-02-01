using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    class SuggestionsFragment : Fragment
    {
        private ProgressBar _progress;
        private ListView _list;
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
            _list = view.FindViewById<ListView>(Resource.Id.listView);
            
            SetAdapter();

            
            return view;
        }

        private async void SetAdapter()
        {
            _progress.Visibility = ViewStates.Visible;
            
            var offices = await Client.GetSuggestions(OfficeId);

            var adapter = new SuggestionsAdapter(Activity, offices.Items);

            _list.Adapter = adapter;
            _list.ItemClick += ListOnItemClick;
            _progress.Visibility = ViewStates.Gone;
        
        }

        private void ListOnItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var intent = new Intent(Application.Context, typeof(CommentsActivity));
            var item = ((SuggestionsAdapter) _list.Adapter)[e.Position];
            intent.PutExtra("name", item.StudentName);
            intent.PutExtra("id", item.Id);
            intent.PutExtra("title", item.Title);
            intent.PutExtra("body", item.Body);

            StartActivity(intent);
        }

        
        
        
    }
}