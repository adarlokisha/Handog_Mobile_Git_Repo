using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;
using System;

namespace Handog_MobileApp
{ // <--- Change to opening bracket
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

        public LoginPage()
        {
            InitializeComponent();
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

                                if (role.Equals("Organizer", StringComparison.OrdinalIgnoreCase))
                                {
                                    await DisplayAlert("Success", $"Logged in as {firstName} ({role})", "OK");
                                    await Navigation.PushAsync(new O_HOME(loggedInAccountNum));
                                }
                                else
                                {
                                    await DisplayAlert("Success", $"Logged in as {firstName} ({role})", "OK");
                                    await Navigation.PushAsync(new V_HOME()); // Ensure V_HOME has a generic constructor or its own parameter if needed!
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
    }
} // <--- Change to closing bracket