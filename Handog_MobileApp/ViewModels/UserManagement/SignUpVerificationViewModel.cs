using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Handog_MobileApp.Models;


namespace Handog_MobileApp.ViewModels.UserManagement
{
    public class SignUpVerificationViewModel : BindableObject
    {
        private readonly string _userEmail;
        private readonly string _sentCode;
        private readonly string _apiBaseUrl = "https://handog-api-crhyajbgcxapfgd3.southeastasia-01.azurewebsites.net/api/account";

        private string _enteredCode;
        public string EnteredCode
        {
            get => _enteredCode;
            set { _enteredCode = value; OnPropertyChanged(); }
        }

        public ICommand VerifyCommand { get; }
        public INavigation Navigation { get; set; }

        public SignUpVerificationViewModel(string email, string code)
        {
            _userEmail = email;
            _sentCode = code;
            VerifyCommand = new Command(async () => await VerifyAsync());
        }

        private async Task VerifyAsync()
        {
            if (string.IsNullOrWhiteSpace(EnteredCode))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter the verification code.", "OK");
                return;
            }

            if (EnteredCode != _sentCode)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Invalid code. Please try again.", "OK");
                return;
            }

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsJsonAsync($"{_apiBaseUrl}/verify", new
                {
                    Email = _userEmail,
                    Code = EnteredCode
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    await Application.Current.MainPage.DisplayAlert("Success", result.Message, "OK");
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Verification failed.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("API Error", ex.Message, "OK");
            }
        }
    }
}
