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

namespace Handog_MobileApp.ViewModels.UserManagement
{
    public class LoginViewModel : BindableObject
    {
        private const string SavedEmailKey = "User Email";
        private readonly string _apiBaseUrl = "https://handog-api-crhyajbgcxapfgd3.southeastasia-01.azurewebsites.net"; // 👈 replace with your Azure API URL

        private string _email;
        private string _password;
        private bool _isPasswordVisible;

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

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set { _isPasswordVisible = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand SignUpCommand { get; }
        public ICommand TogglePasswordCommand { get; }

        public INavigation Navigation { get; set; }

        public LoginViewModel()
        {
            LoadSavedEmail();

            LoginCommand = new Command(async () => await LoginAsync());
            ForgotPasswordCommand = new Command(async () => await Navigation.PushAsync(new ForgotPasswordPage()));
            SignUpCommand = new Command(async () => await Navigation.PushAsync(new SignUpRolePage()));
            TogglePasswordCommand = new Command(() => IsPasswordVisible = !IsPasswordVisible);
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
                    // Extract the exact error message from the backend
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Log it to your Output window for debugging
                    System.Diagnostics.Debug.WriteLine($"[API Error] Status: {response.StatusCode}, Content: {errorContent}");

                    // Display a more specific alert to help you troubleshoot
                    await Application.Current.MainPage.DisplayAlert("Login Failed",
                        $"Status: {response.StatusCode}\nDetails: {errorContent}\n\nPlease check your credentials or account status.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("API Error", ex.Message, "OK");
            }
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
