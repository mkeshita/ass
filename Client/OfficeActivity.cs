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
    [Activity(Theme = "@style/AppTheme.NoActionBar", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class OfficeActivity : AppCompatActivity
    {

        private ImageView _officePicture;
        private TextView _officeShortName, _officeLongName, _officeRatingCount, _officeSuggestions;
        private RatingBar _officeRating,_myRating;
        private Button _viewAllReviews,_viewAllSuggestions,_suggest,_review,_submitReview,
                        _submitSuggestion,_reviews_more, _suggestions_more;
        private LinearLayout _reviews, _suggestions, _reviewProgress,_suggestionProgress;
        private RelativeLayout _reviewView,_suggestionView;
        private EditText _myReview,_suggestionSubject,_suggestionBody;
        private CheckBox _privateBox,_suggestionPrivate;
        private ProgressBar _reviewsProgress, _suggestionsProgress;
        
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
            
            if(Client.SelectedOffice == null)
                Finish();

            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Office);

            _reviewsProgress = FindViewById<ProgressBar>(Resource.Id.reviews_progress);
            _suggestionsProgress = FindViewById<ProgressBar>(Resource.Id.suggestions_progress);
            
            _submitSuggestion = FindViewById<Button>(Resource.Id.submit_suggestion);
            _suggestionProgress = FindViewById<LinearLayout>(Resource.Id.suggestion_progress);
            _suggestionView = FindViewById<RelativeLayout>(Resource.Id.suggestion_view);
            _suggestionSubject = FindViewById<EditText>(Resource.Id.suggestion_subject);
            _suggestionBody = FindViewById<EditText>(Resource.Id.suggestion_body);
            _suggestionPrivate = FindViewById<CheckBox>(Resource.Id.suggestion_private);

            _officePicture = FindViewById<ImageView>(Resource.Id.officePicture);
            _officeShortName = FindViewById<TextView>(Resource.Id.officeShortName);
            _officeLongName = FindViewById<TextView>(Resource.Id.officeLongName);
            _officeSuggestions = FindViewById<TextView>(Resource.Id.officeSuggestions);
            _officeRatingCount = FindViewById<TextView>(Resource.Id.officeRatingCount);
            _officeRating = FindViewById<RatingBar>(Resource.Id.officeRating);

            _viewAllReviews = FindViewById<Button>(Resource.Id.viewAllReviews);
            _viewAllReviews.Visibility = ViewStates.Gone;
            
            _viewAllSuggestions = FindViewById<Button>(Resource.Id.viewAllSuggestions);
            _viewAllSuggestions.Visibility = ViewStates.Gone;
            
            _suggest = FindViewById<Button>(Resource.Id.suggest);
            _review = FindViewById<Button>(Resource.Id.review);
            _reviews = FindViewById<LinearLayout>(Resource.Id.reviews);
            _suggestions = FindViewById<LinearLayout>(Resource.Id.suggestions);
            _privateBox = FindViewById<CheckBox>(Resource.Id.privateCheckbox);

            _reviewView = FindViewById<RelativeLayout>(Resource.Id.review_view);
            _reviewView.Visibility = ViewStates.Gone;
            
            _reviewProgress = FindViewById<LinearLayout>(Resource.Id.reviewProgress);
            _submitReview = FindViewById<Button>(Resource.Id.submit_review);
            _myRating = FindViewById<RatingBar>(Resource.Id.rating);
            _myReview = FindViewById<EditText>(Resource.Id.review_text);

            _suggestions_more = FindViewById<Button>(Resource.Id.suggestions_more);
            _reviews_more = FindViewById<Button>(Resource.Id.reviews_more);
            
            _viewAllSuggestions.Text = "VIEW ALL " + Client.SelectedOffice?.SuggestionsCount;
            _viewAllReviews.Text = "VIEW ALL " + Client.SelectedOffice?.RatingCount;

            SetupOffice();
            
            GetRatings(0);
            GetReviews();
            
            _review.Click += ReviewOnClick;
            _submitReview.Click += SubmitReviewOnClick;
            
            _suggest.Click += SuggestOnClick;
            _submitSuggestion.Click += SubmitSuggestionOnClick;
            
            _reviews_more.Click += ReviewsMoreOnClick;
        }

        private int RatingsPage = -1;
        
        private void ReviewsMoreOnClick(object sender1, EventArgs eventArgs)
        {
            GetRatings(RatingsPage+1);
        }

        private async void SubmitSuggestionOnClick(object sender, EventArgs eventArgs)
        {
            if (_suggestionBody.Text.Length == 0) return;
            if (_suggestionSubject.Text.Length == 0) return;
            
            _review.Enabled = false;
            _suggestionProgress.Visibility = ViewStates.Visible;
            _suggestionPrivate.Visibility = ViewStates.Gone;
            _suggestionSubject.Enabled = false;
            _suggestionBody.Enabled = false;
            _submitSuggestion.Enabled = false;
            
            var result = await Client.Suggest(Client.SelectedOffice.Id, 
                _suggestionSubject.Text, 
                _suggestionBody.Text,
                _suggestionPrivate.Checked);

            _suggestionSubject.Text = "";
            _suggestionBody.Text = "";
            _suggestionPrivate.Checked = false;

            _suggestionProgress.Visibility = ViewStates.Gone;
            _suggestionPrivate.Visibility = Client.Server.AllowPrivateSuggestions ? ViewStates.Visible : ViewStates.Invisible;
            _suggestionBody.Enabled = true;
            _suggestionSubject.Enabled = true;
            _submitSuggestion.Enabled = true;
            _review.Enabled = true;

            var dlg = new Android.App.AlertDialog.Builder(this);
            if (result == null)
            {
                dlg.SetMessage("Please make sure you are connected to the server.");
                dlg.SetTitle("Failed to submit your suggestion. Retry?");
                dlg.SetPositiveButton("RETRY", (o, args) =>
                {
                    SubmitSuggestionOnClick(sender, eventArgs);
                });
                dlg.SetNegativeButton("CANCEL", (o, args) =>
                {

                });
                dlg.Show();
                return;
            }
            dlg.SetTitle("Congratulations!");
            dlg.SetMessage($"Your suggestion has been submitted.");
            dlg.SetCancelable(true);
            dlg.Show();

            _suggestionView.Visibility = ViewStates.Gone;
            _suggest.Enabled = true;
            _officeSuggestions.Text = result.TotalCount.ToString("#,##0");

            if (_suggestions.ChildCount == 7) return;
            _suggestions.RemoveAllViews();
            foreach (var item in result.Items)
            {
                var row = SuggestionsAdapter.GetView(
                    LayoutInflater.Inflate(Resource.Layout.SuggestionRow, null, false), item, this);

                row.Clickable = true;
                row.Click += (s, args) =>
                {
                    Client.SelectedSuggestion = item;
                    StartActivity(typeof(SuggestionView));
                };

                _suggestions.AddView(row);
            }
        }

        private void SuggestOnClick(object sender, EventArgs eventArgs)
        {
            _suggest.Enabled = false;
            _review.Enabled = true;
            _suggestionView.Visibility = ViewStates.Visible;
            _reviewView.Visibility = ViewStates.Gone;
            _suggestionProgress.Visibility = ViewStates.Gone;
            
            if (CurrentFocus != null)
            {
                var imm = (InputMethodManager) GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(CurrentFocus.WindowToken, 0);
            }
            
            _suggestionPrivate.Visibility = Client.Server.AllowPrivateSuggestions ? ViewStates.Visible : ViewStates.Invisible;
        }

        private async void SubmitReviewOnClick(object sender, EventArgs eventArgs)
        {
            if (CurrentFocus != null)
            {
                var imm = (InputMethodManager) GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(CurrentFocus.WindowToken, 0);
            }
            
            _reviewProgress.Visibility = ViewStates.Visible;
            _privateBox.Visibility = ViewStates.Gone;
            _submitReview.Enabled = false;
            _myReview.Enabled = false;
            _myRating.Enabled = false;
            _suggest.Enabled = false;
            
            var result = await Client.RateOffice(Client.SelectedOffice.Id,(int) _myRating.Rating, _myReview.Text, _privateBox.Checked, 7);
            
            _reviewProgress.Visibility = ViewStates.Gone;
            _privateBox.Visibility = Client.Server.AllowPrivateSuggestions ? ViewStates.Visible : ViewStates.Invisible;
            _submitReview.Enabled = true;
            _myReview.Enabled = true;
            _myRating.Enabled = true;
            _suggest.Enabled = true;
            
            var dlg = new Android.App.AlertDialog.Builder(this);
            if (result == null)
            {   
                dlg.SetMessage("Please make sure you are connected to the server.");
                dlg.SetTitle("Failed to submit your review. Retry?");
                dlg.SetPositiveButton("RETRY", (o, args) =>
                {
                    SubmitReviewOnClick(sender,eventArgs);
                });
                dlg.SetNegativeButton("CANCEL", (o, args) =>
                {

                });
                dlg.Show();
                return;
            }
            
            dlg.SetTitle("Congratulations!");
            dlg.SetMessage($"Your review for {Client.SelectedOffice.ShortName} has been submitted.");
            dlg.SetCancelable(true);
            dlg.Show();
            _reviewView.Visibility = ViewStates.Gone;
            _review.Enabled = true;
            
            Client.SelectedOffice.Rating = result.Rating;
            Client.SelectedOffice.RatingCount = result.TotalCount;
            
            _officeRatingCount.Text = result.TotalCount.ToString("#,##0");
            _officeRating.Rating = result.Rating;
            Messenger.Default.Broadcast(Messages.OfficeUpdate, Client.SelectedOffice);
            
            _reviews.RemoveAllViews();
            foreach (var item in result.Ratings)
                _reviews.AddView(
                    RatingsAdapter.GetView(
                        LayoutInflater.Inflate(Resource.Layout.RatingRow, null, false),
                        item,
                        this
                    )
                );
        }

        private void ReviewOnClick(object sender, EventArgs eventArgs)
        {
            _suggestionView.Visibility = ViewStates.Gone;
            _reviewView.Visibility = ViewStates.Visible;
            _reviewProgress.Visibility = ViewStates.Gone;
            _privateBox.Visibility = Client.Server.AllowPrivateSuggestions ? ViewStates.Visible : ViewStates.Invisible;
            _review.Enabled = false;
            _suggest.Enabled = true;
        }

        private async void GetRatings(int page)
        {
            _reviewsProgress.Visibility = ViewStates.Visible;
            _reviews_more.Visibility = ViewStates.Gone;
            
            var result = await Client.GetRatings(Client.SelectedOffice.Id, page);

            _reviewsProgress.Visibility = ViewStates.Gone;
            _reviews_more.Visibility = ViewStates.Visible;

            if (result != null)
            {
                Client.SelectedOffice.Rating = result.Rating;
                Client.SelectedOffice.RatingCount = result.TotalCount;

                _officeRating.Rating = result.Rating;
                _officeRatingCount.Text = result.TotalCount.ToString("#,##0");
                Messenger.Default.Broadcast(Messages.OfficeUpdate, Client.SelectedOffice);

                if (result.Ratings.Count > 0) RatingsPage = page;
                
                foreach (var item in result.Ratings)
                {
                    var row = RatingsAdapter.GetView(
                        LayoutInflater.Inflate(Resource.Layout.RatingRow, null, false),
                        item,
                        this);
                    _reviews.AddView(row);
                }
            }
        }

        private async void GetReviews()
        {
           


            var suggestions = await Client.GetSuggestions(Client.SelectedOffice.Id,7);

            _suggestionsProgress.Visibility = ViewStates.Gone;
            
            if(suggestions!=null)
            foreach (var item in suggestions.Items)
                {
                    var row = SuggestionsAdapter.GetView(
                        LayoutInflater.Inflate(Resource.Layout.SuggestionRow, null, false),item,this);

                    row.Clickable = true;
                    row.Click += (sender, args) =>
                    {
                        Client.SelectedSuggestion = item;
                        StartActivity(typeof(SuggestionView));
                    };
                    
                    _suggestions.AddView(row);
                }

        }
        
        private void SetListViewSize(ListView view)
        {
            var adapter = view.Adapter;
            
            
        }

        private void SetupOffice()
        {
            _officeShortName.Text = Client.SelectedOffice.ShortName;
            _officeLongName.Text = Client.SelectedOffice.LongName;
            _officeRatingCount.Text = Client.SelectedOffice.RatingCount.ToString();
            _officeRating.Rating = Client.SelectedOffice.Rating;
            _officeSuggestions.Text = Client.SelectedOffice.SuggestionsCount.ToString();
            
            var pic = Client.GetOfficePicture(Client.SelectedOffice.Id);

            if(pic != null)
                _officePicture.SetImageBitmap(BitmapFactory.DecodeByteArray(pic.Picture, 0, pic.Picture.Length));
            else
            {
                Messenger.Default.AddListener<OfficePicture>(Messages.OfficePictureReceived,
                    office =>
                    {
                        if(office.OfficeId != Client.SelectedOffice.Id)
                            return;
                        RunOnUiThread(() =>
                            _officePicture.SetImageBitmap(BitmapFactory.DecodeByteArray(office.Picture, 0, office.Picture.Length)));
                    });
            }
        }
    }
}