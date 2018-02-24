using System;
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

            return GetView(view, item, _context);
        }

        public static View GetView(View view, Suggestion item, Activity _context)
        {
            var usr = Client.GetPicture(item.UserId);
            if (usr != null)
                view.FindViewById<ImageView>(Resource.Id.userPicture)
                    .SetImageBitmap(BitmapFactory.DecodeByteArray(usr.Picture, 0, usr.Picture.Length));
            else
            {
                Messenger.Default.AddListener<UserPicture>(Messages.PictureReceived,
                    user =>
                    {
                        if (user.UserId != item.Id)
                            return;
                        _context.RunOnUiThread(() =>
                            view.FindViewById<ImageView>(Resource.Id.userPicture)
                                .SetImageBitmap(BitmapFactory.DecodeByteArray(user.Picture, 0, user.Picture.Length)));
                    });
            }

            view.FindViewById<TextView>(Resource.Id.userName).Text = item.StudentName;
            view.FindViewById<TextView>(Resource.Id.title).Text = item.Title;
            view.FindViewById<TextView>(Resource.Id.comments).Text = item.Comments.ToString();

            var likes = view.FindViewById<TextView>(Resource.Id.likes);
            likes.Text = item.Likes.ToString();
            view.FindViewById<ImageView>(Resource.Id.like)
                .Click += async (sender, args) =>
            {
                if (item.Liked) return;
                item.Liked = true;
                var votes = await Client.LikeSuggestion(item.Id, false);
                var msg = "";
                if (votes == null)
                {
                    msg = "Up vote failed";
                    item.Liked = false;
                }
                else
                {
                    msg = "Up vote successful";
                    _context.RunOnUiThread(()=>likes.Text = votes.ToString());
                    item.Liked = true;
                    item.Disliked = false;
                }
                Toast.MakeText(_context,msg,ToastLength.Short).Show();
            };
            view.FindViewById<ImageView>(Resource.Id.dislike)
                .Click += async (sender, args) =>
            {
                if (item.Disliked) return;
                item.Disliked = true;
                var votes = await Client.LikeSuggestion(item.Id, true);
                var msg = "";
                if (votes == null)
                {
                    msg = "Down vote failed";
                    item.Disliked = false;
                }
                else
                {
                    msg = "Down vote successful";
                    _context.RunOnUiThread(() => likes.Text = votes.ToString());
                    item.Disliked = true;
                    item.Liked = false;
                }
                Toast.MakeText(_context, msg, ToastLength.Short).Show();
            };

            return view;
        }
    }
}