using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    internal class RatingsAdapter : BaseAdapter<OfficeRating>
    {
        private List<OfficeRating> _items;
        private Activity _context;

        public RatingsAdapter(Activity context, List<OfficeRating> items)
        {
            if (items == null)
                items = new List<OfficeRating>();
            _items = items;
            _context = context;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override OfficeRating this[int position] => _items[position];
        public override int Count => _items.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = _items[position];

            var view = convertView ?? _context.LayoutInflater.Inflate(Resource.Layout.RatingRow, null);

            view.FindViewById<TextView>(Resource.Id.name).Text = item.StudentName;
            view.FindViewById<TextView>(Resource.Id.message).Text = item.Message;
            view.FindViewById<RatingBar>(Resource.Id.rating).Rating = item.Rating;

            return view;
        }
    }
}