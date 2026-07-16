using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Handog_MobileApp.Views.Organizer;
using Handog_MobileApp.Views.Volunteer;
using Handog_MobileApp.Views.UserManagement;
using Handog_MobileApp.Models;
using System;

namespace Handog_MobileApp.ViewModels.UserManagement
{
    public class LoginViewModel : BindableObject
    {
        private const string SavedEmailKey = "User Email";
        private readonly string _apiBaseUrl = "https://handog-api-crhyajbgcxapfgd3.southeastasia-01.azurewebsites.net";

        private string _email;
        private string _password;
        private bool _isPasswordVisible;
        private bool _isAppealVisible;
        private string _appealReason;

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        // Handles the Checkbox state
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                _isPasswordVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPasswordHidden)); // Notifies the Entry to update
            }
        }

        // Inverts the visibility for the Entry.IsPassword property
        public bool IsPasswordHidden => !IsPasswordVisible;

        // Handles the Appeal Overlay visibility
        public bool IsAppealVisible
        {
            get => _isAppealVisible;
            set { _isAppealVisible = value; OnPropertyChanged(); }
        }

        public string AppealReason
        {
            get => _appealReason;
            set { _appealReason = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand SignUpCommand { get; }
        public ICommand SubmitAppealCommand { get; }
        public ICommand CancelAppealCommand { get; }

        public INavigation Navigation { get; set; }

        public LoginViewModel()
        {
            LoadSavedEmail();

            LoginCommand = new Command(async () => await LoginAsync());
            ForgotPasswordCommand = new Command(async () => await Navigation.PushAsync(new ForgotPasswordPage()));
            SignUpCommand = new Command(async () => await Navigation.PushAsync(new SignUpRolePage()));

            // Appeal Commands
            SubmitAppealCommand = new Command(async () => await SubmitAppealAsync());
            CancelAppealCommand = new Command(() => IsAppealVisible = false);
        }

        private void LoadSavedEmail()
        {
            if (Preferences.Default.ContainsKey(SavedEmailKey))
            {
                Email = Preferences.Default.Get(SavedEmailKey, string.Empty);
            }
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsJsonAsync($"{_apiBaseUrl}/api/account/login", new { Email, Password });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    Preferences.Default.Set(SavedEmailKey, Email);

                    await Application.Current.MainPage.DisplayAlert("Success", $"Logged in as {result.Firstname} ({result.AccRole})", "OK");

                    if (result.AccRole.Equals("Organizer", StringComparison.OrdinalIgnoreCase))
                        await Navigation.PushAsync(new O_HOME(result.AccountNum));
                    else
                        await Navigation.PushAsync(new V_HOME(result.AccountNum));
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[API Error] Status: {response.StatusCode}, Content: {errorContent}");

                    // Trigger the Appeal Overlay if the backend says the account is banned
                    if (errorContent.Contains("banned", StringComparison.OrdinalIgnoreCase) ||
                        errorContent.Contains("suspended", StringComparison.OrdinalIgnoreCase))
                    {
                        IsAppealVisible = true;
                        return;
                    }

                    await Application.Current.MainPage.DisplayAlert("Login Failed",
                        $"Status: {response.StatusCode}\nDetails: {errorContent}\n\nPlease check your credentials.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("API Error", ex.Message, "OK");
            }
        }

        private async Task SubmitAppealAsync()
        {
            if (string.IsNullOrEmpty(AppealReason))
            {
                await Application.Current.MainPage.DisplayAlert("Required", "Please provide a reason for your appeal.", "OK");
                return;
            }

            // Note: You will need to build the API endpoint for submitting appeals later.
            // For now, this just hides the overlay and shows a success message.
            await Application.Current.MainPage.DisplayAlert("Submitted", "Your appeal has been submitted successfully to the administration team.", "OK");
            AppealReason = string.Empty;
            IsAppealVisible = false;
        }
    }

    public class LoginResponse
    {
        public int AccountNum { get; set; }
        public string AccRole { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Message { get; set; }
    }
}