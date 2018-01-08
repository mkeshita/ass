using System;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
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
        private RatingBar _officeRating;
        private Button _viewAllReviews,_viewAllSuggestions,_suggest,_review;
        private LinearLayout _reviews, _suggestions;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            if(Client.SelectedOffice == null)
                Finish();

            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Office);


            _officePicture = FindViewById<ImageView>(Resource.Id.officePicture);
            _officeShortName = FindViewById<TextView>(Resource.Id.officeShortName);
            _officeLongName = FindViewById<TextView>(Resource.Id.officeLongName);
            _officeSuggestions = FindViewById<TextView>(Resource.Id.officeSuggestions);
            _officeRatingCount = FindViewById<TextView>(Resource.Id.officeRatingCount);
            _officeRating = FindViewById<RatingBar>(Resource.Id.officeRating);
            _viewAllReviews = FindViewById<Button>(Resource.Id.viewAllReviews);
            _viewAllSuggestions = FindViewById<Button>(Resource.Id.viewAllSuggestions);
            _suggest = FindViewById<Button>(Resource.Id.suggest);
            _review = FindViewById<Button>(Resource.Id.review);
            _reviews = FindViewById<LinearLayout>(Resource.Id.reviews);
            _suggestions = FindViewById<LinearLayout>(Resource.Id.suggestions);


            _viewAllSuggestions.Text = "VIEW ALL " + Client.SelectedOffice?.SuggestionsCount;
            _viewAllReviews.Text = "VIEW ALL " + Client.SelectedOffice?.RatingCount;

            SetupOffice();
            GetReviews();
        }

        private async void GetReviews()
        {
            var result = await Client.GetRatings(Client.SelectedOffice.Id,7);
            foreach (var item in result.Ratings)
                _reviews.AddView(
                    RatingsAdapter.GetView(
                        LayoutInflater.Inflate(Resource.Layout.RatingRow, null, false),
                        item,
                        this
                    )
                );


            var suggestions = await Client.GetSuggestions(Client.SelectedOffice.Id,7);
            
            foreach (var item in suggestions.Items)
                _suggestions.AddView(
                    SuggestionsAdapter.GetView(
                        LayoutInflater.Inflate(Resource.Layout.SuggestionRow, null, false),
                        item,
                        this
                    )
                );

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