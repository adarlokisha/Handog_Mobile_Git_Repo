using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp
{
    public partial class O_PROPOSALS : ContentPage
    {
        private readonly int currentOrganizerAccountNum;
        private int currentOrganizerLocaleNum;

        // This holds the currently selected active row context instead of a model
        private DataRow selectedProposalContext = null;

        // Change the collection type to DataRow so it mirrors your DB structure automatically
        public ObservableCollection<DataRow> ActiveProposalsCollection { get; set; } = new ObservableCollection<DataRow>();

        public O_PROPOSALS(int sessionAccountNum)
        {
            InitializeComponent();
            this.currentOrganizerAccountNum = sessionAccountNum;
            BindingContext = this;
        }

        // 1. Add this call to your existing OnAppearing()
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await FetchOrganizerName(); // New call
            await FetchPendingProposalsFromDatabase();
        }

        // 2. Add this method to your class
        private async Task FetchOrganizerName()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    string sql = "SELECT Firstname FROM ACCOUNT WHERE AccountNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", currentOrganizerAccountNum);
                        await conn.OpenAsync();
                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null)
                        {
                            MainThread.BeginInvokeOnMainThread(() => {
                                LblHeaderUsername.Text = $"{result}!";
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading name: {ex.Message}");
            }
        }

        private async Task FetchPendingProposalsFromDatabase()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    string sql = @"SELECT p.ProposalNum, p.Proposal_ID, p.AccountNum, p.CategoryNum,
                                          p.ProposalTitle, p.ProposalDetails, p.PreferredDate,
                                          p.PreferredStartTime, p.PreferredEndTime, p.ProposalStatus,
                                          (a.Firstname + ' ' + a.Lastname) AS ProposerName
                                   FROM EVENTPROPOSAL p
                                   INNER JOIN ACCOUNT a ON p.AccountNum = a.AccountNum
                                   WHERE p.ProposalStatus = 'Pending'
                                   ORDER BY p.ProposalNum ASC";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();

                        // Use a DataTable to load raw structure from SQL Server
                        DataTable dt = new DataTable();
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            dt.Load(reader);
                        }

                        // Safely feed the rows straight into your collection on the UI Thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ActiveProposalsCollection.Clear();
                            foreach (DataRow row in dt.Rows)
                            {
                                ActiveProposalsCollection.Add(row);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Sync Fault", $"Could not load proposals: {ex.Message}", "OK");
                });
            }
        }

        private async void OnAcceptProposalClicked(object sender, EventArgs e)
        {
            if ((sender as ImageButton)?.CommandParameter is DataRow row)
            {
                selectedProposalContext = row;
                ClearFormInputFields();

                await PreFillFormWithOrganizerAndProposalDetails(row);
                PopupCreateModal.IsVisible = true;
            }
        }

        private async void OnRejectProposalClicked(object sender, EventArgs e)
        {
            if ((sender as ImageButton)?.CommandParameter is DataRow row)
            {
                bool confirm = await DisplayAlert("Reject Proposal", "Are you sure you want to update this proposal status to Rejected?", "Reject", "Cancel");
                if (!confirm) return;

                try
                {
                    using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                    {
                        string sql = "UPDATE EVENTPROPOSAL SET ProposalStatus = 'Rejected' WHERE ProposalNum = @ProposalNum";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ProposalNum", row["ProposalNum"]);
                            await conn.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Remove from list visually
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ActiveProposalsCollection.Remove(row);
                    });
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Database Failure", ex.Message, "OK");
                }
            }
        }

        private async Task PreFillFormWithOrganizerAndProposalDetails(DataRow row)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    string orgSql = @"SELECT a.Firstname, a.Lastname, a.Email, a.ContactNum, a.LocaleNum, l.LocaleName 
                                     FROM ACCOUNT a
                                     INNER JOIN LOCALE l ON a.LocaleNum = l.LocaleNum
                                     WHERE a.AccountNum = @AccNum";

                    using (SqlCommand cmd = new SqlCommand(orgSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", currentOrganizerAccountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                TxtOrgName.Text = $"{reader["Firstname"]} {reader["Lastname"]}";
                                TxtEmail.Text = reader["Email"].ToString();
                                TxtContact.Text = reader["ContactNum"].ToString();
                                currentOrganizerLocaleNum = Convert.ToInt32(reader["LocaleNum"]);
                                TxtLocaleDisplay.Text = reader["LocaleName"].ToString();
                            }
                        }
                    }
                }

                // Extract directly by using database index column names strings
                TxtTitle.Text = row["ProposalTitle"].ToString();
                PickerDate.Date = Convert.ToDateTime(row["PreferredDate"]);
                PickerStart.Time = (TimeSpan)row["PreferredStartTime"];
                PickerEnd.Time = (TimeSpan)row["PreferredEndTime"];
                TxtAnnouncement.Text = $"This event was generated from Proposal {row["Proposal_ID"]}.\nDetails:\n{row["ProposalDetails"]}";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Pre-fill Error", ex.Message, "OK");
            }
        }

        private async void OnPublishEventClicked(object sender, EventArgs e)
        {
            LblCreateError.IsVisible = false;

            string title = TxtTitle.Text?.Trim();
            string headOrganizer = TxtOrgName.Text?.Trim();
            string announcement = TxtAnnouncement.Text?.Trim();
            string expectedStr = TxtExpectedParts.Text?.Trim();
            string maxVolStr = TxtMaxVol.Text?.Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(headOrganizer) ||
                string.IsNullOrEmpty(expectedStr) || string.IsNullOrEmpty(maxVolStr) || string.IsNullOrEmpty(announcement))
            {
                ShowFormErrorMessage("Please complete all required input parameters.");
                return;
            }

            if (!int.TryParse(expectedStr, out int expectedParticipants) || !int.TryParse(maxVolStr, out int maxVolCapacity))
            {
                ShowFormErrorMessage("Participants and Capacity inputs must be numeric values.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    string idQuery = "SELECT ISNULL(MAX(EventNum), 0) + 1 FROM EVENT";
                    int nextNum = 1;
                    using (SqlCommand idCmd = new SqlCommand(idQuery, conn))
                    {
                        nextNum = (int)await idCmd.ExecuteScalarAsync();
                    }
                    string generatedEventID = $"EVN{nextNum:D5}";

                    string insertEventSql = @"
                        INSERT INTO EVENT
                        (Event_ID, OrganizerNum, ProposalNum, CategoryNum, LocaleNum, EventTitle, 
                         EventDescription, EventDate, StartTime, EndTime, ExpectedParticipants, VolunteerCapacity, EventStatus)
                        VALUES
                        (@EventID, @OrganizerNum, @ProposalNum, @CategoryNum, @LocaleNum, @EventTitle, 
                         @EventDesc, @EventDate, @StartTime, @EndTime, @Expected, @Capacity, 'Published')";

                    using (SqlCommand cmd = new SqlCommand(insertEventSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@EventID", generatedEventID);
                        cmd.Parameters.AddWithValue("@OrganizerNum", currentOrganizerAccountNum);
                        cmd.Parameters.AddWithValue("@ProposalNum", selectedProposalContext["ProposalNum"]);
                        cmd.Parameters.AddWithValue("@CategoryNum", selectedProposalContext["CategoryNum"]);
                        cmd.Parameters.AddWithValue("@LocaleNum", currentOrganizerLocaleNum);
                        cmd.Parameters.AddWithValue("@EventTitle", title);
                        cmd.Parameters.AddWithValue("@EventDesc", announcement);
                        cmd.Parameters.Add(new SqlParameter("@EventDate", SqlDbType.Date) { Value = PickerDate.Date });
                        cmd.Parameters.Add(new SqlParameter("@StartTime", SqlDbType.Time) { Value = PickerStart.Time });
                        cmd.Parameters.Add(new SqlParameter("@EndTime", SqlDbType.Time) { Value = PickerEnd.Time });
                        cmd.Parameters.AddWithValue("@Expected", expectedParticipants);
                        cmd.Parameters.AddWithValue("@Capacity", maxVolCapacity);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    string updateProposalSql = "UPDATE EVENTPROPOSAL SET ProposalStatus = 'Approved' WHERE ProposalNum = @ProposalNum";
                    using (SqlCommand cmd = new SqlCommand(updateProposalSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProposalNum", selectedProposalContext["ProposalNum"]);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                PopupCreateModal.IsVisible = false;
                await DisplayAlert("Success", "Event successfully published and proposal accepted!", "OK");
                await FetchPendingProposalsFromDatabase();
            }
            catch (Exception ex)
            {
                ShowFormErrorMessage($"Database Error: {ex.Message}");
            }
        }

        private void ShowFormErrorMessage(string msg)
        {
            LblCreateError.Text = msg;
            LblCreateError.IsVisible = true;
        }

        private void ClearFormInputFields()
        {
            TxtTitle.Text = TxtAnnouncement.Text = TxtMaxVol.Text = TxtExpectedParts.Text = "";
            LblCreateError.IsVisible = false;
        }

        private void OnCloseModalClicked(object sender, EventArgs e) => PopupCreateModal.IsVisible = false;

        // --- NAVIGATION HANDLERS ---
        private async void HomeBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new O_HOME(currentOrganizerAccountNum));
        }

        private async void EventsBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new O_EVENTS(currentOrganizerAccountNum));
        }

        private async void ProfileBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new O_PROFILE(currentOrganizerAccountNum));
        }
    }
}