using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using norsu.ass.Network;

namespace norsu.ass
{
    [Activity(Icon = "@drawable/ic_launcher", Label = "Registration", Theme = "@style/AppTheme",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, NoHistory = true)]
    public class RegisterActivity : AppCompatActivity
    {
        private EditText _studentId, _studentName, _password, _password2,_course, _lastname;
        private Button _cancel, _submit;
        private LinearLayout _registrationForm;
        private RelativeLayout _registrationProgress;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            var dlg = new Android.Support.V7.App.AlertDialog.Builder(this);
            if (Client.Server == null)
            {
                dlg.SetTitle("Connection to server is not established.");
                dlg.SetMessage("Please make sure you are connected to the server and try again.");
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
                    dlg = new Android.Support.V7.App.AlertDialog.Builder(this);
                    dlg.SetMessage("Disconnected from server.");
                    dlg.SetMessage("The server has shutdown. Please try again later.");
                    dlg.SetPositiveButton("EXIT", (sender, args) =>
                    {
                        FinishAffinity();
                    });
                    dlg.SetCancelable(false);
                    dlg.Show();
                });
            });
            
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Register);

            _studentId = FindViewById<EditText>(Resource.Id.student_id);
            _studentName = FindViewById<EditText>(Resource.Id.name);
            _password = FindViewById<EditText>(Resource.Id.password1);
            _password2 = FindViewById<EditText>(Resource.Id.password2);
            _cancel = FindViewById<Button>(Resource.Id.cancel);
            _submit = FindViewById<Button>(Resource.Id.submit);
            _registrationForm = FindViewById<LinearLayout>(Resource.Id.registration_form);
            _registrationProgress = FindViewById<RelativeLayout>(Resource.Id.registration_progress);
            _course = FindViewById<EditText>(Resource.Id.course);
            _lastname = FindViewById<EditText>(Resource.Id.last_name);
            _cancel.Click += CancelOnClick;
            _submit.Click += SubmitOnClick;
            
            if(savedInstanceState!=null)
                OnRestoreInstanceState(savedInstanceState);

           
        }


        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("reg_id", _studentId.Text);
            outState.PutString("reg_name",_studentName.Text);
            outState.PutString("reg_ln",_lastname.Text);
            outState.PutString("reg_course",_course.Text);
            outState.PutBoolean("reg_proc",_registrationProgress.Visibility==ViewStates.Visible);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnRestoreInstanceState(Bundle s)
        {
            base.OnRestoreInstanceState(s);
            if (s == null) return;
            _studentId.Text = s.GetString("reg_id");
            _studentName.Text = s.GetString("reg_name");
            _lastname.Text = s.GetString("reg_ln");
            _course.Text = s.GetString("reg_course");
            if (s.GetBoolean("reg_proc"))
            {
                _registrationProgress.Visibility = ViewStates.Visible;
                _registrationForm.Enabled = false;
            }
            else
            {
                _registrationProgress.Visibility = ViewStates.Gone;
                _registrationForm.Enabled = true;
            }
        }

        private async void SubmitOnClick(object sender, EventArgs eventArgs)
        {
            _registrationProgress.Visibility = ViewStates.Visible;
            _registrationForm.Enabled = false;

            var msg = "";
            if (string.IsNullOrWhiteSpace(_studentId.Text))
                msg = "Student ID is required";
            if (_password.Text.Length == 0)
                msg = "Password is required";
            if (_password.Text != _password2.Text)
                msg = "Password does not match";
            if (Client.Server.FullnameRequired)
            {
                if (string.IsNullOrWhiteSpace(_studentName.Text))
                    msg = "First name is required";
                if (string.IsNullOrWhiteSpace(_lastname.Text))
                    msg = "Last name is required";
            }

            if (msg != "")
            {
                Toast.MakeText(this, msg, ToastLength.Short).Show();
                return;
            }
            
            var result = await Client.Register(_studentId.Text, _password.Text, _studentName.Text,_lastname.Text, _course.Text);
            _registrationProgress.Visibility = ViewStates.Gone;
            _registrationForm.Enabled = true;
            
            if (result==null)
            {
                Toast.MakeText(this, "Registration Failed",ToastLength.Short).Show();
                return;
            }

            if (!result.Success)
            {
                Toast.MakeText(this, result.Message, ToastLength.Short).Show();
                return;
            }
            
            StartActivity(new Intent(Application.Context, typeof(OfficesActivity)));
            Finish();
        }

        private void CancelOnClick(object sender, EventArgs eventArgs)
        {
            StartActivity(typeof(LoginActivity));
            Finish();
        }

    }
}