using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    class OfficesAdapter : BaseAdapter<Office>
    {
        private List<Office> _items;
        private Activity _context;

        public OfficesAdapter(Activity context, List<Office> items)
        {
            if (items == null)
                items = new List<Office>();
            _items = items;
            _context = context;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override Office this[int position] => _items[position];
        public override int Count => _items.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = _items[position];

            var view = convertView ?? _context.LayoutInflater.Inflate(Resource.Layout.OfficesRow, null);

            view.FindViewById<TextView>(Resource.Id.name).Text = item.ShortName;
            view.FindViewById<RatingBar>(Resource.Id.rating).Rating = item.Rating;

            return view;
        }
    }
}