using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Handog_MobileApp.Models; // 👈 reference the shared ApiResponse model

namespace Handog_MobileApp.ViewModels.UserManagement
{
    public class SignUpViewModel : BindableObject
    {
        private readonly string _role;
        private readonly string _apiBaseUrl = "https://handog-api-crhyajbgcxapfgd3.southeastasia-01.azurewebsites.net/api/account";

        private string _firstName, _lastName, _email, _contact, _password, _confirmPassword, _locale;

        public string FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(); } }
        public string LastName { get => _lastName; set { _lastName = value; OnPropertyChanged(); } }
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        public string Contact { get => _contact; set { _contact = value; OnPropertyChanged(); } }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }
        public string ConfirmPassword { get => _confirmPassword; set { _confirmPassword = value; OnPropertyChanged(); } }
        public string Locale { get => _locale; set { _locale = value; OnPropertyChanged(); } }

        public ICommand SignUpCommand { get; }
        public INavigation Navigation { get; set; }

        public SignUpViewModel(string role)
        {
            _role = role;
            SignUpCommand = new Command(async () => await SignUpAsync());
        }

        private async Task SignUpAsync()
        {
            // 1. Validation
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Contact) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "All fields are required.", "OK");
                return;
            }

            if (_role == "Organizer" && string.IsNullOrWhiteSpace(Locale))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Locale is required for Organizers.", "OK");
                return;
            }

            if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Invalid email format.", "OK");
                return;
            }

            if (!Regex.IsMatch(Contact, @"^[0-9]{10,15}$"))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Invalid contact number.", "OK");
                return;
            }

            if (!ValidatePassword(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Password does not meet complexity requirements.", "OK");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            // 2. Call Azure API
            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsJsonAsync($"{_apiBaseUrl}/signup", new
                {
                    FirstName,
                    LastName,
                    Email,
                    Contact,
                    Password,
                    Role = _role,
                    Locale = _role == "Organizer" ? Locale : null
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

                    await Application.Current.MainPage.DisplayAlert("Success", result.Message, "OK");

                    // 👇 Navigate to verification page if API returns a code
                    if (!string.IsNullOrEmpty(result.VerificationCode))
                    {
                        await Navigation.PushAsync(new Views.UserManagement.SignUpVerificationPage(Email, result.VerificationCode));
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Signup failed. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("API Error", ex.Message, "OK");
            }
        }

        private bool ValidatePassword(string password)
        {
            return password.Length >= 8 && password.Length <= 32 &&
                   Regex.IsMatch(password, @"[A-Z]") &&
                   Regex.IsMatch(password, @"[a-z]") &&
                   Regex.IsMatch(password, @"[0-9]") &&
                   Regex.IsMatch(password, @"[!@#$%^&*]");
        }
    }
}
