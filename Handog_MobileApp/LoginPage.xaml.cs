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
            "Server = tcp:handogmobile.database.windows.net,1433;" +
            "Initial Catalog = handog-mobile-v3; Persist Security Info=False;" +
            "User ID = handogadmin; Password=HandogMobileDB!; " +
            "MultipleActiveResultSets=False;" +
            "Encrypt=True;" +
            "TrustServerCertificate=False;" +
            "Connection Timeout = 30;";

        private const string SavedEmailKey = "UserEmail";

        // Temporarily store the ID of the banned user attempting to log in
        private int _bannedAccountNum = 0;

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

                    // We removed the "AccountStatus = 'Active'" filter so we can detect banned accounts in the C# logic
                    string query = @"SELECT AccountNum, AccRole, Firstname, Lastname, AccountStatus 
                                     FROM ACCOUNT 
                                     WHERE Email = @Email AND AccPassword = @Password";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Password", password);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string status = reader["AccountStatus"].ToString();
                                int accountNum = Convert.ToInt32(reader["AccountNum"]);

                                if (status == "Banned")
                                {
                                    _bannedAccountNum = accountNum;
                                    AppealOverlay.IsVisible = true; // Show the appeal form
                                    return;
                                }

                                if (status != "Active")
                                {
                                    await DisplayAlert("Login Failed", "Your account is currently inactive.", "OK");
                                    return;
                                }

                                // If Active, proceed normally
                                string role = reader["AccRole"].ToString();
                                string firstName = reader["Firstname"].ToString();

                                Preferences.Default.Set(SavedEmailKey, email);

                                if (role.Equals("Organizer", StringComparison.OrdinalIgnoreCase))
                                {
                                    await DisplayAlert("Success", $"Logged in as {firstName} ({role})", "OK");
                                    await Navigation.PushAsync(new O_HOME(accountNum));
                                }
                                else
                                {
                                    await DisplayAlert("Success", $"Logged in as {firstName} ({role})", "OK");
                                    await Navigation.PushAsync(new V_HOME(accountNum));
                                }
                            }
                            else
                            {
                                await DisplayAlert("Login Failed", "Invalid email or password.", "OK");
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

        private async void OnSubmitAppeal_Clicked(object sender, EventArgs e)
        {
            string reason = AppealReasonEditor.Text?.Trim();

            if (string.IsNullOrEmpty(reason))
            {
                await DisplayAlert("Required", "Please provide a reason for your appeal.", "OK");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if an appeal is already pending to prevent spam
                    string checkQuery = "SELECT COUNT(*) FROM BAN_APPEAL WHERE AccountNum = @AccNum AND Status = 'Pending'";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@AccNum", _bannedAccountNum);
                        int existingAppeals = (int)await checkCmd.ExecuteScalarAsync();
                        if (existingAppeals > 0)
                        {
                            await DisplayAlert("Notice", "You already have a pending appeal. Please wait for an administrator to review it.", "OK");
                            AppealOverlay.IsVisible = false;
                            return;
                        }
                    }

                    string insertQuery = @"INSERT INTO BAN_APPEAL (AccountNum, AppealReason, Status, DateSubmitted) 
                                           VALUES (@AccNum, @Reason, 'Pending', GETDATE())";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", _bannedAccountNum);
                        cmd.Parameters.AddWithValue("@Reason", reason);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await DisplayAlert("Submitted", "Your appeal has been submitted successfully to the administration team.", "OK");
                    AppealReasonEditor.Text = string.Empty;
                    AppealOverlay.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void OnCancelAppeal_Clicked(object sender, EventArgs e)
        {
            AppealReasonEditor.Text = string.Empty;
            AppealOverlay.IsVisible = false;
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