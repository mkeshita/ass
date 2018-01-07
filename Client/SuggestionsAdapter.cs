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

            var usr = Client.GetPicture(item.UserId);
            if (usr != null)
                view.FindViewById<ImageView>(Resource.Id.userPicture)
                    .SetImageBitmap(BitmapFactory.DecodeByteArray(usr.Picture, 0, usr.Picture.Length));
            else
            {
                Messenger.Default.AddListener<UserPicture>(Messages.PictureReceived, 
                    user =>
                    {
                        if (user.UserId != item.Id) return;
                        _context.RunOnUiThread(()=>
                            view.FindViewById<ImageView>(Resource.Id.userPicture)
                                .SetImageBitmap(BitmapFactory.DecodeByteArray(user.Picture, 0, user.Picture.Length)));
                    });
            }
            
            view.FindViewById<TextView>(Resource.Id.userName).Text = item.StudentName;
            view.FindViewById<TextView>(Resource.Id.title).Text = item.Title;
            view.FindViewById<TextView>(Resource.Id.likes).Text = item.Likes.ToString();

            return view;
        }
    }
}