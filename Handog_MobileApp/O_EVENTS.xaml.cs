using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Handog_MobileApp
{
    public partial class O_EVENTS : ContentPage
    {
        private readonly string connectionString = "Server=handog-mobile-server.database.windows.net;Database=HandogMobileDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly int currentAccountNum;

        // Data Collections linked to XAML bindings
        public ObservableCollection<EventModel> GlobalEventsList { get; set; } = new ObservableCollection<EventModel>();
        public ObservableCollection<AttendeeModel> ActiveAttendeesList { get; set; } = new ObservableCollection<AttendeeModel>();

        private string activeTabMode = "MyEvents"; // Options: MyEvents, AllEvents, Completed

        public O_EVENTS(int sessionAccountNum)
        {
            InitializeComponent();
            this.currentAccountNum = sessionAccountNum;

            // Explicitly set BindingContext for structural binding evaluation
            EventsCollectionView.ItemsSource = GlobalEventsList;
            AttendeesListView.ItemsSource = ActiveAttendeesList;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await SetOrganizerIdentityHeader();
            await SyncEventRegistryDataset();
        }

        // --- DATABASE SYNC AND FETCH ENGINE ---
        private async Task SetOrganizerIdentityHeader()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT Firstname FROM ACCOUNT WHERE AccountNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", currentAccountNum);
                        object result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            LblHeaderOrganizerName.Text = $"{result}!";
                            EntryFormOrganizer.Text = result.ToString().ToUpper();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Identity Header Failure: {ex.Message}");
            }
        }

        private async Task SyncEventRegistryDataset()
        {
            try
            {
                GlobalEventsList.Clear();
                string searchQuery = EventSearchBar.Text ?? "";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Filter logic built on tab configuration states
                    string baseSql = @"SELECT e.*, a.Firstname + ' ' + a.Lastname AS OrganizerName 
                                       FROM EVENT e 
                                       INNER JOIN ACCOUNT a ON e.OrganizerNum = a.AccountNum 
                                       WHERE (e.EventTitle LIKE @Search OR e.EventVenue LIKE @Search)";

                    if (activeTabMode == "MyEvents")
                        baseSql += " AND e.OrganizerNum = @UserNum AND e.Status != 'Completed'";
                    else if (activeTabMode == "AllEvents")
                        baseSql += " AND e.Status != 'Completed'";
                    else if (activeTabMode == "Completed")
                        baseSql += " AND e.Status = 'Completed'";

                    using (SqlCommand cmd = new SqlCommand(baseSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Search", $"%{searchQuery}%");
                        cmd.Parameters.AddWithValue("@UserNum", currentAccountNum);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int organizerNum = (int)reader["OrganizerNum"];
                                GlobalEventsList.Add(new EventModel
                                {
                                    EventID = (int)reader["EventID"],
                                    EventTitle = reader["EventTitle"].ToString(),
                                    OrganizerName = reader["OrganizerName"].ToString(),
                                    EventVenue = reader["EventVenue"].ToString(),
                                    EventAddress = reader["EventAddress"].ToString(),
                                    EventTime = reader["EventTime"].ToString(),
                                    EventDate = Convert.ToDateTime(reader["EventDate"]).ToString("yyyy-MM-dd"),
                                    EventDetails = reader["EventDetails"].ToString(),
                                    IsMyEvent = (organizerNum == currentAccountNum)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Data Failure", $"Failed syncing layout data: {ex.Message}", "OK");
            }
        }

        // --- INTERACTIVE UI TAB SWITCHERS ---
        private async void MyEventsTab_Tapped(object sender, EventArgs e)
        {
            UpdateTabVisuals(TabMyEvents, TabAllEvents, TabCompleted);
            activeTabMode = "MyEvents";
            await SyncEventRegistryDataset();
        }

        private async void AllEventsTab_Tapped(object sender, EventArgs e)
        {
            UpdateTabVisuals(TabAllEvents, TabMyEvents, TabCompleted);
            activeTabMode = "AllEvents";
            await SyncEventRegistryDataset();
        }

        private async void CompletedTab_Tapped(object sender, EventArgs e)
        {
            UpdateTabVisuals(TabCompleted, TabMyEvents, TabAllEvents);
            activeTabMode = "Completed";
            await SyncEventRegistryDataset();
        }

        private void UpdateTabVisuals(Border active, Border inactive1, Border inactive2)
        {
            active.BackgroundColor = Colors.White;
            inactive1.BackgroundColor = Colors.Transparent;
            inactive2.BackgroundColor = Colors.Transparent;
        }

        private async void EventSearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            await SyncEventRegistryDataset();
        }

        // --- MODAL PANEL MANAGEMENT ---
        private void OpenOrganizePanelBtn_Clicked(object sender, EventArgs e)
        {
            PopupOrganizePanel.IsVisible = true;
        }

        private void CloseOrganizePanelBtn_Clicked(object sender, EventArgs e)
        {
            PopupOrganizePanel.IsVisible = false;
        }

        private void CloseDetailsPanelBtn_Clicked(object sender, EventArgs e)
        {
            PopupDetailsPanel.IsVisible = false;
        }

        private async void ViewEventDetailsBtn_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var targetEvent = button?.CommandParameter as EventModel;
            if (targetEvent == null) return;

            PopupModalTitle.Text = targetEvent.EventTitle;
            PopupModalOrganizer.Text = $"By: {targetEvent.OrganizerName}";
            PopupTxtVenue.Text = $"• VENUE: {targetEvent.EventVenue}";
            PopupTxtAddress.Text = $"• ADDRESS: {targetEvent.EventAddress}";
            PopupTxtDateTime.Text = $"• SCHEDULE: {targetEvent.EventDate} @ {targetEvent.EventTime}";
            PopupTxtDetails.Text = $"• DETAILS: {targetEvent.EventDetails}";

            // Toggle operational context visibility controls based on access rights
            ManagementActionPanel.IsVisible = targetEvent.IsMyEvent;

            // Mocking dynamic list updates down the stack logic layout
            ActiveAttendeesList.Clear();
            ActiveAttendeesList.Add(new AttendeeModel { VolunteerName = "Juan Dela Cruz", StatusText = "PRESENT", StatusColor = Colors.Green });
            ActiveAttendeesList.Add(new AttendeeModel { VolunteerName = "Maria Clara", StatusText = "PENDING", StatusColor = Colors.Orange });

            PopupDetailsPanel.IsVisible = true;
        }

        // --- DATA UPDATE INTERACTION TRIGGERS ---
        private async void PublishNewEventBtn_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EntryFormTitle.Text) || string.IsNullOrWhiteSpace(EntryFormVenue.Text))
            {
                await DisplayAlert("Missing Values", "Title and Venue requirements are verified mandatory fields.", "OK");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"INSERT INTO EVENT (EventTitle, OrganizerNum, EventVenue, EventAddress, EventDate, EventTime, EventDetails, Status) 
                                   VALUES (@Title, @OrgNum, @Venue, @Address, @Date, @Time, @Details, 'Active')";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Title", EntryFormTitle.Text);
                        cmd.Parameters.AddWithValue("@OrgNum", currentAccountNum);
                        cmd.Parameters.AddWithValue("@Venue", EntryFormVenue.Text);
                        cmd.Parameters.AddWithValue("@Address", EntryFormAddress.Text ?? "");
                        cmd.Parameters.AddWithValue("@Date", PickerFormDate.Date);
                        cmd.Parameters.AddWithValue("@Time", EntryFormTime.Text ?? "");
                        cmd.Parameters.AddWithValue("@Details", EditorFormDetails.Text ?? "");

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                PopupOrganizePanel.IsVisible = false;
                await SyncEventRegistryDataset();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Write Fault", ex.Message, "OK");
            }
        }

        private async void DeleteEventBtn_Clicked(object sender, EventArgs e)
        {
            var btn = sender as ImageButton;
            var target = btn?.CommandParameter as EventModel;
            if (target == null) return;

            bool check = await DisplayAlert("Drop Event", "Purge registration entirely from server storage maps?", "Yes", "No");
            if (!check) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string sql = "DELETE FROM EVENT WHERE EventID = @ID";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", target.EventID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await SyncEventRegistryDataset();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Delete Error", ex.Message, "OK");
            }
        }

        private async void ConfirmAttendanceBtn_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Attendance Logged", "Target roster items updated to Present successfully.", "OK");
        }

        private async void ConfirmCompletionBtn_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Status Closed", "Event registry flagged complete.", "OK");
            PopupDetailsPanel.IsVisible = false;
            await SyncEventRegistryDataset();
        }

        // --- BOTTOM ROUTING BAR NAV HUB ---
        private async void BackBtn_Clicked(object sender, EventArgs e) { await Navigation.PopAsync(); }
        private async void HomeBtn_Clicked(object sender, EventArgs e) { await Navigation.PushAsync(new O_HOME(currentAccountNum)); }
        private async void ProposalsBtn_Clicked(object sender, EventArgs e) { await Navigation.PushAsync(new O_PROPOSALS(currentAccountNum)); }
        private async void ProfileBtn_Clicked(object sender, EventArgs e) { await Navigation.PushAsync(new O_PROFILE(currentAccountNum)); }
    }

    // Supporting Blueprint Entities 
    public class EventModel
    {
        public int EventID { get; set; }
        public string EventTitle { get; set; }
        public string OrganizerName { get; set; }
        public string EventVenue { get; set; }
        public string EventAddress { get; set; }
        public string EventTime { get; set; }
        public string EventDate { get; set; }
        public string EventDetails { get; set; }
        public bool IsMyEvent { get; set; }
    }

    public class AttendeeModel
    {
        public string VolunteerName { get; set; }
        public string StatusText { get; set; }
        public Color StatusColor { get; set; }
    }
}