using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;
//using Microsoft.UI.Xaml;
using System;
//using Windows.System;

namespace Handog_MobileApp;

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
            // Clear validation / Loader implementation could go here
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Query to safely fetch data matching the entered Email and Password
                string query = @"SELECT AccRole, Firstname, Lastname 
                                     FROM ACCOUNT 
                                     WHERE Email = @Email AND AccPassword = @Password AND AccountStatus = 'Active'";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Parameterized to prevent SQL injection vulnerabilities
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // User found successfully! Retrieve role and meta details
                            string role = reader["AccRole"].ToString();
                            string firstName = reader["Firstname"].ToString();

                            if (role.Equals("Organizer", StringComparison.OrdinalIgnoreCase))
                            {
                                // Route seamlessly to your Organizer Dashboard page
                                await DisplayAlert("Success", $"Logged in as {firstName} ({role})", "OK");
                                await Navigation.PushAsync(new O_HOME());
                            }
                            else
                            {
                                // Fallback route for generic volunteers/admins down the road
                                await DisplayAlert("Success", $"Logged in as {firstName} ({role})", "OK");
                                await Navigation.PushAsync(new V_HOME());
                            }
                        }
                        else
                        {
                            await DisplayAlert("Login Failed", "Invalid email, password, or account is pending/banned.", "OK");
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