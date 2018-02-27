using System;
using System.Collections.Generic;
using Android.App;
using Android.Graphics;
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

            return GetView(convertView, item, _context);
        }

        public static View GetView(View view,OfficeRating item, Activity _context)
        {
            view.FindViewById<TextView>(Resource.Id.name).Text = item.StudentName;
            view.FindViewById<TextView>(Resource.Id.message).Text = item.Message;
            view.FindViewById<RatingBar>(Resource.Id.rating).Rating = item.Rating;

            var usr = Client.GetPicture(item.UserId);
            if (usr != null)

            {
                try
                {
                    var pic = BitmapFactory.DecodeByteArray(usr.Picture, 0, usr.Picture.Length);
                    if (pic == null)
                        return view;
                    view.FindViewById<ImageView>(Resource.Id.picture)
                        .SetImageBitmap(pic);
                }
                catch (Exception e)
                {
                    //
                }
            }
            else
            {
                Messenger.Default.AddListener<UserPicture>(Messages.PictureReceived,
                    user =>
                    {
                        if (user.UserId != item.UserId)
                            return;
                        _context.RunOnUiThread(() =>
                            view.FindViewById<ImageView>(Resource.Id.picture)
                                .SetImageBitmap(BitmapFactory.DecodeByteArray(user.Picture, 0, user.Picture.Length)));
                    });
            }
            return view;
        }
    }
}