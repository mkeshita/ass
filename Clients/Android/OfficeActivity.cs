using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Text;
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
        private TextView _officeShortName, _officeLongName, _officeRatingCount, _officeSuggestions,
                        _suggestionTitleLeft,_suggestionBodyLeft;
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
            _suggestionTitleLeft = FindViewById<TextView>(Resource.Id.suggestion_subject_left);
            _suggestionBodyLeft = FindViewById<TextView>(Resource.Id.suggestion_body_left);

            _suggestionTitleLeft.Text = $"0/{Client.Server.SuggestionTitleMin} Characters";
            _suggestionBodyLeft.Text = $"0/{Client.Server.SuggestionBodyMin} Characters";
            _suggestionSubject.TextChanged += SuggestionSubjectOnTextChanged;
            _suggestionBody.TextChanged += SuggestionSubjectOnTextChanged;

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
            GetSuggestions(0);
            
            _review.Click += ReviewOnClick;
            _submitReview.Click += SubmitReviewOnClick;
            
            _suggest.Click += SuggestOnClick;
            _submitSuggestion.Click += SubmitSuggestionOnClick;
            
            _reviews_more.Click += ReviewsMoreOnClick;
            _suggestions_more.Click+= SuggestionsMoreOnClick;
        }
        
        private void SuggestionSubjectOnTextChanged(object sender1, TextChangedEventArgs textChangedEventArgs)
        {
            _submitSuggestion.Enabled = true;
            var count = _suggestionSubject.Text.Length;
            if (count < Client.Server.SuggestionTitleMin)
            {
                _suggestionTitleLeft.Text = $"{count}/{Client.Server.SuggestionTitleMin} Characters";
                _submitSuggestion.Enabled = false;
            }
            else
            {
                var left = Client.Server.SuggestionTitleMax - count;
                if (left < 0)
                {
                    _submitSuggestion.Enabled = false;
                }
                _suggestionTitleLeft.Text = $"{left} Characters Left";
            }

            count = _suggestionBody.Text.Length;
            if (count < Client.Server.SuggestionBodyMin)
            {
                _suggestionBodyLeft.Text = $"{count}/{Client.Server.SuggestionBodyMin} Characters";
                _submitSuggestion.Enabled = false;
            }
            else
            {
                var left = Client.Server.SuggestionBodyMax - count;
                if (left < 0)
                {
                    _submitSuggestion.Enabled = false;
                }
                _suggestionBodyLeft.Text = $"{left} Characters Left";
            }
        }

        private void SuggestionsMoreOnClick(object sender1, EventArgs eventArgs)
        {
            GetSuggestions(SuggestionsPage+1);
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


            if (result?.Success ?? false)
            {
                Toast.MakeText(this, "Suggestion submitted", ToastLength.Short).Show();

                _suggestionView.Visibility = ViewStates.Gone;
                _suggest.Enabled = true;
                _officeSuggestions.Text = result.TotalCount.ToString("#,##0");
                
                AddSuggestionRow(result.Result);
            }
                else
            {
                var dlg = new Android.App.AlertDialog.Builder(this);
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
            
            if (result == null)
            {
                var dlg = new Android.App.AlertDialog.Builder(this);
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

            Toast.MakeText(this, "Review submitted", ToastLength.Short).Show();
            
            UpdateMyRating(_myRating.Rating, _myReview.Text);
            
            _reviewView.Visibility = ViewStates.Gone;
            _review.Enabled = true;
            
        }

        private void UpdateMyRating(float rating, string message)
        {
            if (MyRatingView == null) return;
            _officeRating.Rating = Client.SelectedOffice.Rating;
            Client.SelectedOffice.Rating = (Client.SelectedOffice.Rating + _myRating.Rating) / 2f;
            if (MyRatingView.Visibility == ViewStates.Gone)
            {
                _officeRatingCount.Text = (int.Parse($"0{_officeRatingCount.Text}") + 1).ToString();
                Client.SelectedOffice.RatingCount++;
            }
            MyRatingView.FindViewById<TextView>(Resource.Id.message).Text = message;
            MyRatingView.FindViewById<RatingBar>(Resource.Id.rating).Rating = rating;
            MyRatingView.Visibility = ViewStates.Visible;

            Messenger.Default.Broadcast(Messages.OfficeUpdate, Client.SelectedOffice);
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

        private View MyRatingView;
        private int LastRatingIndex = -1;
        private async void GetRatings(int page)
        {
            _reviewsProgress.Visibility = ViewStates.Visible;
            _reviews_more.Visibility = ViewStates.Invisible;
            
            var result = await Client.GetRatings(Client.SelectedOffice.Id, page);
            
            if (result != null)
            {
                Client.SelectedOffice.Rating = result.Rating;
                Client.SelectedOffice.RatingCount = result.TotalCount;

                _officeRating.Rating = result.Rating;
                _officeRatingCount.Text = result.TotalCount.ToString("#,##0");
                Messenger.Default.Broadcast(Messages.OfficeUpdate, Client.SelectedOffice);
                
                for (var i = LastRatingIndex+1; i < result.Ratings.Count; i++)
                {
                    var item = result.Ratings[i];
                    var row = RatingsAdapter.GetView(
                        LayoutInflater.Inflate(Resource.Layout.RatingRow, null, false),
                        item,
                        this);
                    if (item.MyRating)
                    {
                        MyRatingView = row;
                        if (item.Rating == 0)
                            MyRatingView.Visibility = ViewStates.Gone;
                    }
                    _reviews.AddView(row);
                }

                LastRatingIndex = result.Ratings.Count - 1;
                
                if(result.Ratings.Count == 7)
                {
                    RatingsPage = page;
                    LastRatingIndex = -1;
                }
            }

            _reviewsProgress.Visibility = ViewStates.Gone;
            _reviews_more.Visibility = ViewStates.Visible;
        }

        private int SuggestionsPage = -1;
        private int LastSuggestionIndex = -1;
        private List<long> LoadedSuggestions = new List<long>();
        
        private async void GetSuggestions(int page)
        {
            _suggestions_more.Visibility = ViewStates.Invisible;
            _suggestionsProgress.Visibility = ViewStates.Visible;
            var result = await Client.GetSuggestions(Client.SelectedOffice.Id,page);
            _suggestions_more.Visibility = ViewStates.Visible;
            _suggestionsProgress.Visibility = ViewStates.Gone;

            if (result == null) return;
            
            for (var i = LastSuggestionIndex+1; i < result.Items.Count; i++)
            {
                var item = result.Items[i];

                if (LoadedSuggestions.Contains(item.Id))
                    continue;
                
                AddSuggestionRow(item);
            }

            if (result.Full)
            {
                LastSuggestionIndex = -1;
                if (SuggestionsPage < result.Page)
                    SuggestionsPage = result.Page;
            }
            else
            {
                LastSuggestionIndex = result.Items.Count - 1;
            }

            

        }

        private void AddSuggestionRow(Suggestion item)
        {
            
            LoadedSuggestions.Add(item.Id);

            var row = SuggestionsAdapter
                .GetView(LayoutInflater.Inflate(Resource.Layout.SuggestionRow, null, false), item, this);

            row.Clickable = true;
            row.Click += (sender, args) =>
            {
                Client.SelectedSuggestion = item;
                StartActivity(typeof(SuggestionView));
            };

            _suggestions.AddView(row);
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

            if (pic != null)
            {
                try
                {
                    _officePicture.SetImageBitmap(BitmapFactory.DecodeByteArray(pic.Picture, 0, pic.Picture.Length));
                }
                catch (Exception e)
                {
                    //
                }
                
            }
            else
            {
                Messenger.Default.AddListener<OfficePicture>(Messages.OfficePictureReceived,
                    office =>
                    {
                        if(office.OfficeId != Client.SelectedOffice.Id)
                            return;
                        RunOnUiThread(() =>
                        {
                            try
                            {
                                _officePicture.SetImageBitmap(
                                    BitmapFactory.DecodeByteArray(office.Picture, 0, office.Picture.Length));
                            }
                            catch (Exception e)
                            {
                                //
                            }
                            
                        });
                    });
            }
        }
    }
}