using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    [Activity(Theme = "@style/AppTheme.NoActionBar",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class SuggestionView : AppCompatActivity
    {
        private TextView _title, _body, _votes, _studentName, _studentType;
        private ImageView _studentPicture, _voteUp, _voteDown;
        private LinearLayout _comments;
        private Button _sendComment;
        private EditText _myComment;
        private ProgressBar _commentProgress;
        private ScrollView _scrollView;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            var dlg = new Android.App.AlertDialog.Builder(this);
            if (Client.Server == null)
            {
                dlg.SetTitle(Resource.String.no_server_title);
                dlg.SetMessage(Resource.String.no_server_message);
                dlg.SetNegativeButton("Exit", (sender, args) =>
                {
                    FinishAffinity();
                });
                dlg.Show();
                return;
            }
            Messenger.Default.AddListener(Messages.Shutdown, () =>
            {
                RunOnUiThread(() =>
                {
                    try
                    {
                        if (CurrentFocus != null)
                        {
                            var imm = (InputMethodManager) GetSystemService(Context.InputMethodService);
                            imm.HideSoftInputFromWindow(CurrentFocus.WindowToken, 0);
                        }
                        dlg = new Android.App.AlertDialog.Builder(this);
                            dlg.SetTitle(Resource.String.server_shutdown_title);
                            dlg.SetMessage(Resource.String.server_shutdown_message);
                            dlg.SetPositiveButton("Exit", (sender, args) =>
                            {
                                FinishAffinity();
                            });
                            dlg.SetCancelable(false);
                            dlg.Show();
                   
                    }
                    catch (Exception e)
                    {
                        FinishAffinity();
                    }
                });
            });
            
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SuggestionView);

            _title = FindViewById<TextView>(Resource.Id.title);
            _body = FindViewById<TextView>(Resource.Id.body);
            _votes = FindViewById<TextView>(Resource.Id.likes);
            _studentName = FindViewById<TextView>(Resource.Id.userName);
            _studentType = FindViewById<TextView>(Resource.Id.userType);
            _studentPicture = FindViewById<ImageView>(Resource.Id.userPicture);
            _voteUp = FindViewById<ImageView>(Resource.Id.like);
            _voteDown = FindViewById<ImageView>(Resource.Id.dislike);
            _comments = FindViewById<LinearLayout>(Resource.Id.comments);
            _sendComment = FindViewById<Button>(Resource.Id.send_comment);
            _myComment = FindViewById<EditText>(Resource.Id.my_comment);
            _commentProgress = FindViewById<ProgressBar>(Resource.Id.comment_progress);
            _sendComment.Click += SendCommentOnClick;
            _scrollView = FindViewById<ScrollView>(Resource.Id.scrollview);

            if (!Client.SelectedSuggestion.AllowComment)
            {
                FindViewById<LinearLayout>(Resource.Id.comment_view).Visibility = ViewStates.Gone;
            }
            
            SetupValues();
            SetupVotingHandler();
            FetchComments();
        }

        private async void SendCommentOnClick(object o, EventArgs eventArgs)
        {
            if (string.IsNullOrWhiteSpace(_myComment.Text)) return;
            _commentProgress.Visibility = ViewStates.Visible;
            _myComment.Enabled = false;
            _sendComment.Enabled = false;
            var result = await Client.AddComment(Client.SelectedSuggestion.Id, _myComment.Text);
            if (result)
            {
                var comment = new Comment()
                {
                    Message = _myComment.Text,
                    Sender = Client.Username,
                    SuggestionId = Client.SelectedSuggestion.Id,
                    UserId = Client.UserId,
                    Time = DateTime.Now,
                };
                var view = CommentsAdapter.GetView(comment, null, this);
                _comments.AddView(view);
                _myComment.Text = "";

                var child = FindViewById<LinearLayout>(Resource.Id.scrollview_child);
                
                _scrollView.ScrollTo(0, child.Height);
            }
            
            _commentProgress.Visibility = ViewStates.Gone;
            _myComment.Enabled = true;
            _sendComment.Enabled = true;
        }

        private async void FetchComments()
        {
            var res = await Client.GetComments(Client.SelectedSuggestion.Id);
            _comments.RemoveAllViews();

            if (res == null) return;
            foreach (var comment in res.Items)
            {
                _comments.AddView(CommentsAdapter.GetView(comment,null,this));
            }
        }

        private void SetupVotingHandler()
        {
            var item = Client.SelectedSuggestion;
            _voteUp.Click += async (sender, args) =>
            {
                if (item.Liked)
                    return;
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
                    RunOnUiThread(() => _votes.Text = votes.ToString());
                    item.Liked = true;
                    item.Disliked = false;
                }
                Toast.MakeText(this, msg, ToastLength.Short).Show();
            };
            _voteDown.Click += async (sender, args) =>
            {
                if (item.Disliked)
                    return;
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
                    RunOnUiThread(() => _votes.Text = votes.ToString());
                    item.Disliked = true;
                    item.Liked = false;
                }
                Toast.MakeText(this, msg, ToastLength.Short).Show();
            };
        }

        private void SetupValues()
        {
            _title.Text = Client.SelectedSuggestion.Title;
            _body.Text = Client.SelectedSuggestion.Body;
            _votes.Text = Client.SelectedSuggestion.Likes.ToString("#,##0");
            _studentName.Text = Client.SelectedSuggestion.StudentName;

            var item = Client.SelectedSuggestion;
            var usr = Client.GetPicture(item.UserId);
            if (usr != null)
                _studentPicture.SetImageBitmap(BitmapFactory.DecodeByteArray(usr.Picture, 0, usr.Picture.Length));
            else
            {
                Messenger.Default.AddListener<UserPicture>(Messages.PictureReceived,
                    user =>
                    {
                        if (user.UserId != item.Id) return;
                        RunOnUiThread(() =>
                            _studentPicture.SetImageBitmap(BitmapFactory.DecodeByteArray(user.Picture, 0, user.Picture.Length)));
                    });
            }
        }
    }
}