using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Handog_MobileApp
{
    public partial class O_PROFILE : ContentPage
    {
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

            // Safety Check: If the ID is 0, it means it wasn't passed correctly from the previous page
            if (currentOrganizerAccountNum == 0)
            {
                await DisplayAlert("Error", "Account ID missing. The profile cannot load.", "OK");
                return;
            }

            await LoadTargetAccountProfileInformation();
        }

        private async Task LoadTargetAccountProfileInformation()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. Fetch personal details
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

                    // 2. Count "Organized" (Total events ever created by this organizer)
                    string organizedCountSql = "SELECT COUNT(*) FROM EVENT WHERE OrganizerNum = @AccNum";
                    using (SqlCommand cmdOrg = new SqlCommand(organizedCountSql, conn))
                    {
                        cmdOrg.Parameters.AddWithValue("@AccNum", currentOrganizerAccountNum);
                        int organizedCount = (int)await cmdOrg.ExecuteScalarAsync();
                        LblCountOrganized.Text = organizedCount.ToString();
                    }

                    // 3. Count "Joined" (Total events successfully COMPLETED by this organizer)
                    string joinedCountSql = "SELECT COUNT(*) FROM EVENT WHERE OrganizerNum = @AccNum AND EventStatus = 'Completed'";
                    using (SqlCommand cmdJoined = new SqlCommand(joinedCountSql, conn))
                    {
                        cmdJoined.Parameters.AddWithValue("@AccNum", currentOrganizerAccountNum);
                        int joinedCount = (int)await cmdJoined.ExecuteScalarAsync();
                        LblCountJoined.Text = joinedCount.ToString();
                    }

                    // 4. Count "Absences" (Leaving this at 0, unless you want to track 'Cancelled' events here!)
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