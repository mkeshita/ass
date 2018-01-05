using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    [Activity(Icon = "@drawable/ic_launcher", Label = "Sign In", Theme = "@style/Theme",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, NoHistory = true)]
    public class LoginActivity : Activity
    {
        private Button _loginButton;
        private Button _register;
        private RelativeLayout _progressView;
        private LinearLayout _userView;
        private EditText _username;
        private EditText _password;
        private EditText _nickName;
        private CheckBox _anonymous;
        
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Login);

            _loginButton = FindViewById<Button>(Resource.Id.login);
            _loginButton.Click += LoginButtonOnClick;

            _anonymous = FindViewById<CheckBox>(Resource.Id.anonymous);
            _userView = FindViewById<LinearLayout>(Resource.Id.userView);
            _nickName = FindViewById<EditText>(Resource.Id.nickName);
            _progressView = FindViewById<RelativeLayout>(Resource.Id.progress);
            _username = FindViewById<EditText>(Resource.Id.userName);
            _password = FindViewById<EditText>(Resource.Id.password);
            
            _anonymous.CheckedChange += AnonymousOnCheckedChange;

            if (Client.Server != null)
            {
                _anonymous.Visibility = Client.Server.AllowAnnonymous ? ViewStates.Visible : ViewStates.Gone;
                _register.Visibility = Client.Server.AllowRegistration ? ViewStates.Visible : ViewStates.Gone;
                
            }
        }

        private void AnonymousOnCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs checkedChangeEventArgs)
        {
            if (_anonymous.Checked)
            {
                _userView.Visibility = ViewStates.Gone;
                _nickName.Visibility = ViewStates.Visible;
            }
            else
            {
                _userView.Visibility = ViewStates.Visible;
                _nickName.Visibility = ViewStates.Gone;
            }
        }

        private async void LoginButtonOnClick(object sender, EventArgs eventArgs)
        {
            var usr = _anonymous.Checked ? _nickName.Text : _username.Text;
            if (string.IsNullOrEmpty(usr)) return;
            
            _progressView.Visibility = ViewStates.Visible;

            var result = await Client.Login(usr, _password.Text, _anonymous.Checked);

            _progressView.Visibility = ViewStates.Gone;
            if (result?.Success ?? false)
            {
                StartActivity(new Intent(Application.Context,typeof(OfficesActivity)));
                Finish();
            }
            else
            {
                new AlertDialog.Builder(this)
                    .SetTitle("Login Failed")
                    .Show();
                _username.SelectAll();
                _username.RequestFocus();
            }
        }
    }
}