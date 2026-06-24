using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Handog_MobileApp
{
    public partial class O_PROFILE : ContentPage
    {
        private readonly string connectionString = "Server=handog-mobile-server.database.windows.net;Database=HandogMobileDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly int currentOrganizerAccountNum;

        // Constructor tracking active context parameters directly
        public O_PROFILE(int sessionAccountNum)
        {
            InitializeComponent();
            this.currentOrganizerAccountNum = sessionAccountNum;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadTargetAccountProfileInformation();
        }

        private async Task LoadTargetAccountProfileInformation()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // 1. Fetch personal details from account entity mapping
                    string profileSql = "SELECT Account_ID, Firstname, Lastname FROM ACCOUNT WHERE AccountNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(profileSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", currentOrganizerAccountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string firstName = reader["Firstname"].ToString();
                                string lastName = reader["Lastname"].ToString();

                                LblHeaderUsername.Text = $"{firstName}!";
                                LblFullName.Text = $"{firstName} {lastName}".ToUpper();
                                LblAccountID.Text = reader["Account_ID"].ToString();
                            }
                        }
                    }

                    // 2. Aggregate count metrics from EVENT table maps for organized events
                    string organizedCountSql = "SELECT COUNT(*) FROM EVENT WHERE OrganizerNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(organizedCountSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", currentOrganizerAccountNum);
                        int organizedCount = (int)await cmd.ExecuteScalarAsync();
                        LblCountOrganized.Text = organizedCount.ToString();
                    }

                    // 3. Optional tracking placeholders for tracking standard metrics
                    LblCountJoined.Text = "0";
                    LblCountAbsences.Text = "0";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Database Fault", $"Could not load profile statistics: {ex.Message}", "OK");
            }
        }

        private async void BackBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void HomeBtn_Clicked(object sender, EventArgs e)
        {
            // Propagate active user index parameter context down view routes
            await Navigation.PushAsync(new O_HOME(currentOrganizerAccountNum));
        }

        private async void ProposalsBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new O_PROPOSALS(currentOrganizerAccountNum));
        }

        private async void EventsBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new O_EVENTS(currentOrganizerAccountNum));
        }

        private async void LogoutBtn_Clicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout Confirmation", "Are you sure you want to exit your session?", "Logout", "Cancel");
            if (confirm)
            {
                await Navigation.PopToRootAsync();
            }
        }
    }
}