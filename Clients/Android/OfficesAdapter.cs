using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
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

            view.FindViewById<TextView>(Resource.Id.officeShortName).Text = item.ShortName;
            view.FindViewById<TextView>(Resource.Id.officeLongName).Text = item.LongName;
            var ratingBar  = view.FindViewById<RatingBar>(Resource.Id.officeRating);
            ratingBar.Rating = item.Rating;
            
            var ratingCount = view.FindViewById<TextView>(Resource.Id.officeRatingCount);
            ratingCount.Text = item.RatingCount.ToString();

            var pic = view.FindViewById<ImageView>(Resource.Id.officePicture);
            var usr = Client.GetOfficePicture(item.Id);
            if (usr != null)
                pic.SetImageBitmap(BitmapFactory.DecodeByteArray(usr.Picture, 0, usr.Picture.Length));
            else
            {
                Messenger.Default.AddListener<OfficePicture>(Messages.PictureReceived,
                    office =>
                    {
                        if (office.OfficeId != item.Id) return;
                        _context.RunOnUiThread(() =>
                            pic.SetImageBitmap(BitmapFactory.DecodeByteArray(office.Picture, 0, office.Picture.Length)));
                    });
            }

            Messenger.Default.AddListener<Office>(Messages.OfficeUpdate,
                office =>
                {
                    if (office.Id != item.Id)
                        return;
                    _context.RunOnUiThread(() =>
                    {
                        ratingBar.Rating = office.Rating;
                        ratingCount.Text = office.RatingCount.ToString("#,##0");
                        
                    });
                });
            
            return view;
        }
    }
}