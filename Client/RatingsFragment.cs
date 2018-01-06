using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    class RatingsFragment : ListFragment
    {
        private ProgressBar _progress;
        private ProgressBar _submitProgress;
        private RatingBar _rating;
        private EditText _message;
        private CheckBox _privateCheckBox;
        private Button _submit;
        
        public RatingsFragment(long officeId)
        {
            OfficeId = officeId;
        }
        
        public long OfficeId { get; set; }
        
        
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            var view = inflater.Inflate(Resource.Layout.RatingsTab, container, false);
            _progress = view.FindViewById<ProgressBar>(Resource.Id.progress);
            _submitProgress = view.FindViewById<ProgressBar>(Resource.Id.submitProgress);
            _rating = view.FindViewById<RatingBar>(Resource.Id.rating);
            _message = view.FindViewById<EditText>(Resource.Id.message);
            _privateCheckBox = view.FindViewById<CheckBox>(Resource.Id.privateCheckbox);
            _submit = view.FindViewById<Button>(Resource.Id.submit);
            
            _submit.Click += SubmitOnClick;

            SetAdapter();

            return view;
        }

        private async void SetAdapter()
        {
            var offices = await Client.GetRatings(OfficeId);

            var adapter = new RatingsAdapter(Activity, offices.Ratings);

            ListAdapter = adapter;

            _progress.Visibility = ViewStates.Gone;
            var myRating = offices.Ratings.FirstOrDefault(x => x.MyRating);
            if (myRating != null)
            {
                _rating.Rating = myRating.Rating;
                _message.Text = myRating.Message;
            }
        }

        private async void SubmitOnClick(object sender, EventArgs eventArgs)
        {
            _submitProgress.Visibility = ViewStates.Visible;
            var result =
                await Client.RateOffice(OfficeId, (int) _rating.Rating, _message.Text, _privateCheckBox.Checked);
            _submitProgress.Visibility = ViewStates.Gone;
            var dlg = new AlertDialog.Builder(Activity);
            if (result == null)
            {
                dlg.SetTitle("Cannot access server");
            }
            else
            {
                dlg.SetTitle("Your rating has been successfully submitted.");
                ListAdapter = new RatingsAdapter(Activity,result.Ratings);
            }
            dlg.Show();
        }

    }
}