using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using Handog_MobileApp.Views.Organizer;
using Handog_MobileApp.Views.Volunteer;

namespace Handog_MobileApp
{
    public partial class LoginPage : ContentPage
    {
        private readonly string _connectionString =
            "Server = tcp:handog-mobile-server.database.windows.net,1433;" +
            "Initial Catalog = HandogMobileDB; Persist Security Info=False;" +
            "User ID = handogmobileadmin; Password=password123!!; " +
            "MultipleActiveResultSets=False;" +
            "Encrypt=True;" +
            "TrustServerCertificate=False;" +
            "Connection Timeout = 30;";

        private const string SavedEmailKey = "UserEmail";

        public LoginPage()
        {
            InitializeComponent();

            LoadSavedEmail();
        }

        private void LoadSavedEmail()
        {
            if (Preferences.Default.ContainsKey(SavedEmailKey))
            {
                string savedEmail = Preferences.Default.Get(SavedEmailKey, string.Empty);
                EmailEntry.Text = savedEmail;
            }
        }

        private async void OnLogin_Clicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"SELECT AccountNum, AccRole, Firstname, Lastname 
                                     FROM ACCOUNT 
                                     WHERE Email = @Email AND AccPassword = @Password AND AccountStatus = 'Active'";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Password", password);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int loggedInAccountNum = Convert.ToInt32(reader["AccountNum"]);
                                string role = reader["AccRole"].ToString();
                                string firstName = reader["Firstname"].ToString();

                                Preferences.Default.Set(SavedEmailKey, email);

                                if (role.Equals("Organizer", StringComparison.OrdinalIgnoreCase))
                                {
                                    await DisplayAlert("Success", $"Logged in as {firstName} ({role})", "OK");
                                    await Navigation.PushAsync(new O_HOME(loggedInAccountNum));
                                }
                                else
                                {
                                    await DisplayAlert("Success", $"Logged in as {firstName} ({role})", "OK");
                                    await Navigation.PushAsync(new V_HOME(loggedInAccountNum));
                                }
                            }
                            else
                            {
                                await DisplayAlert("Login Failed", "Invalid email, password, or account is inactive.", "OK");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Database Connection Error", ex.Message, "OK");
            }
        }

        private void OnShowPasswordChecked(object sender, CheckedChangedEventArgs e)
        {
            PasswordEntry.IsPassword = !e.Value;
        }

        private async void OnForgotPasswordTapped(object sender, EventArgs e)
        {
            //await Navigation.PushAsync(new ForgotPasswordPage());
        }

        private async void OnSignUpTapped(object sender, EventArgs e)
        {
            //await Navigation.PushAsync(new SignUpPage());
        }
    }
}