using System.Collections.Generic;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    internal class CommentsAdapter : BaseAdapter<Comment>
    {
        private List<Comment> _items;
        private Activity _context;

        public CommentsAdapter(Activity context, List<Comment> items)
        {
            if (items == null)
                items = new List<Comment>();
            _items = items;
            _context = context;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override Comment this[int position] => _items[position];
        public override int Count => _items.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = _items[position];

            var view = convertView ?? _context.LayoutInflater.Inflate(Resource.Layout.CommentRow, null);
            
            view.FindViewById<TextView>(Resource.Id.name).Text = item.Sender;
            view.FindViewById<TextView>(Resource.Id.comment).Text = item.Message;
            
            return view;
        }
    }
}