using System.Collections.Generic;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    internal class SuggestionsAdapter : BaseAdapter<Suggestion>
    {
        private List<Suggestion> _items;
        private Activity _context;

        public SuggestionsAdapter(Activity context, List<Suggestion> items)
        {
            if (items == null)
                items = new List<Suggestion>();
            _items = items;
            _context = context;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override Suggestion this[int position] => _items[position];
        public override int Count => _items.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = _items[position];

            var view = convertView ?? _context.LayoutInflater.Inflate(Resource.Layout.SuggestionRow, null);
            
            view.FindViewById<TextView>(Resource.Id.name).Text = item.StudentName;
            view.FindViewById<TextView>(Resource.Id.title).Text = item.Title;
            view.FindViewById<TextView>(Resource.Id.body).Text = item.Body;
            view.FindViewById<TextView>(Resource.Id.dislikes).Text = item.Dislikes.ToString();
            view.FindViewById<TextView>(Resource.Id.likes).Text = item.Likes.ToString();
            view.FindViewById<ImageButton>(Resource.Id.like).Click += async (sender, args) =>
            {
                if (await Client.LikeSuggestion(item.Id, false))
                {
                    Toast.MakeText(_context, "Successfully liked!", ToastLength.Short);
                }
            };
            view.FindViewById<ImageButton>(Resource.Id.dislike).Click += async (sender, args) =>
            {
                if (await Client.LikeSuggestion(item.Id, true))
                {
                    Toast.MakeText(_context, "Successfully disliked!", ToastLength.Short);
                }
            };
            return view;
        }
    }
}