using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Handog_MobileApp.Models;



namespace Handog_MobileApp.ViewModels.UserManagement
{
    public class ForgotPasswordViewModel : BindableObject
    {
        private readonly string _apiBaseUrl = "https://handog-api-crhyajbgcxapfgd3.southeastasia-01.azurewebsites.net/api/account";

        private string _email;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public ICommand ResetCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public INavigation Navigation { get; set; }

        public ForgotPasswordViewModel()
        {
            ResetCommand = new Command(async () => await ResetPasswordAsync());
            BackToLoginCommand = new Command(async () => await Navigation.PopAsync());
        }

        private async Task ResetPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter your email address.", "OK");
                return;
            }

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsJsonAsync($"{_apiBaseUrl}/forgotpassword", new { Email });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    await Application.Current.MainPage.DisplayAlert("Success", result.Message, "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Unable to process reset request.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("API Error", ex.Message, "OK");
            }
        }
    }
}
