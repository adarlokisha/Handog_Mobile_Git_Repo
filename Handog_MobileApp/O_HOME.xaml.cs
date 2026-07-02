using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Handog_MobileApp
{
    public partial class O_HOME : ContentPage
    {
        // Tracks the runtime user sequence context passed during authentication
        private int currentAccountNum;

        // 1. UPDATED CONSTRUCTOR: Forces the page to receive the active user's Primary Key
        public O_HOME(int accountNum)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            this.currentAccountNum = accountNum;
        }

        // 2. LIFECYCLE HOOK: Triggers whenever this screen appears on the mobile viewport
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Route Guard Check: Bounces back to login screen if no context is found
            if (currentAccountNum <= 0)
            {
                await DisplayAlert("Access Denied", "No active account profile detected. Redirecting to login...", "OK");
                await Navigation.PopToRootAsync();
                return;
            }

            // Fire off background query pipelines if valid
            await LoadDashboardMetricsFromDatabase();
        }

        // 3. DATABASE PIPELINE: Gathers name parameters and aggregates attendance rates
        private async Task LoadDashboardMetricsFromDatabase()
        {
            try
            {
                // Swapped to use your centralized AppConfig connection string!
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    // Query A: Extract Logged-In Account First Name Profile parameters
                    // Query A: Extract Logged-In Account First Name Profile parameters
                    string accountQuery = "SELECT Firstname FROM ACCOUNT WHERE AccountNum = @AccountNum";
                    using (SqlCommand cmd = new SqlCommand(accountQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountNum", currentAccountNum);
                        var result = await cmd.ExecuteScalarAsync();

                        // Force MAUI to update the visual UI on the main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (result != null && result != DBNull.Value && !string.IsNullOrWhiteSpace(result.ToString()))
                            {
                                // Success: Sets the name label directly
                                LblOrganizerName.Text = result.ToString();
                            }
                            else
                            {
                                // Fallback: If this shows up, the database query returned nothing!
                                LblOrganizerName.Text = "Organizer";

                                // Temporary debug alert so you know if the ID passed correctly
                                DisplayAlert("Debug Warning", $"No name found in DB for AccountNum: {currentAccountNum}", "OK");
                            }
                        });
                    } // Command A closes and disposes completely here

                    // Query B: Aggregate Operational Attendance Metrics across past reports
                    string metricsQuery = @"
                        SELECT 
                            ISNULL(SUM(TotalExpected), 0) as OverallExpected, 
                            ISNULL(SUM(TotalPresent), 0) as OverallPresent 
                        FROM EVENTREPORT 
                        WHERE GeneratedBy = @AccountNum";

                    using (SqlCommand cmd = new SqlCommand(metricsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountNum", currentAccountNum);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int expected = Convert.ToInt32(reader["OverallExpected"]);
                                int present = Convert.ToInt32(reader["OverallPresent"]);

                                if (expected > 0)
                                {
                                    // Calculate percentage rate formula 
                                    double rate = ((double)present / expected) * 100;

                                    LblAttendanceSummary.Text = $"{present} out of {expected} attended your events!";
                                    LblAttendancePercentage.Text = $"{Math.Round(rate)}%";
                                }
                                else
                                {
                                    // Default layout view configuration if user has 0 reports logged
                                    LblAttendanceSummary.Text = "No events concluded yet.";
                                    LblAttendancePercentage.Text = "0%";
                                }
                            }
                        } // DataReader safely disposes here
                    }
                }
            }
            catch (Exception ex)
            {
                // Force the UI to show us the exact error on the physical phone screen
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    LblOrganizerName.Text = "Error!";
                    LblAttendanceSummary.Text = "Metrics sync currently offline.";

                    // This popup will reveal exactly what Azure/Android is complaining about
                    await DisplayAlert("Hidden DB Error", ex.Message, "OK");
                });
            }
        }

        // 4. INTERACTIVE BUTTON NAVIGATION AND FEEDBACK LINKS
        private async void OrganizeEventBtn_Clicked(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                await btn.ScaleTo(0.95, 50, Easing.Linear);
                await btn.ScaleTo(1.0, 50, Easing.Linear);
            }
            await Navigation.PushAsync(new O_EVENTS(currentAccountNum));
        }

        private async void ProposalsBtn_Clicked(object sender, EventArgs e)
        {
            await AnimateButton(sender as ImageButton);
            await Navigation.PushAsync(new O_PROPOSALS(currentAccountNum));
        }

        private async void EventsBtn_Clicked(object sender, EventArgs e)
        {
            await AnimateButton(sender as ImageButton);
            await Navigation.PushAsync(new O_EVENTS(currentAccountNum));
        }

        private async void ProfileBtn_Clicked(object sender, EventArgs e)
        {
            await AnimateButton(sender as ImageButton);
            await Navigation.PushAsync(new O_PROFILE(currentAccountNum));
        }

        private async Task AnimateButton(ImageButton button)
        {
            if (button != null)
            {
                await button.ScaleTo(0.92, 50, Easing.Linear);
                await button.ScaleTo(1.0, 50, Easing.Linear);
            }
        }
    }
}