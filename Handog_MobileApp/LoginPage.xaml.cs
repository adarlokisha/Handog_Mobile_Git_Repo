using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;
using System;

namespace Handog_MobileApp;

public partial class LoginPage : ContentPage
{
    private readonly string _connectionString = "Server=10.0.2.2,1433;Database=HANDOG_MOBILE;User Id=sa;Password=password123;TrustServerCertificate=True;";

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
                                await Navigation.PushAsync(new O_HOME());
                            }
                            else
                            {
                                // Fallback route for generic volunteers/admins down the road
                                await DisplayAlert("Success", $"Logged in as {firstName} ({role})", "OK");
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
            // Catches database engine exceptions, timeout errors, or bad connection string properties
            await DisplayAlert("Database Connection Error", ex.Message, "OK");
        }

    }
}