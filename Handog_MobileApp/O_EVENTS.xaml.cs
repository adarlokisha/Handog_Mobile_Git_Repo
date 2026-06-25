using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Handog_MobileApp
{
    public partial class O_EVENTS : ContentPage
    {
        private readonly int currentAccountNum;
        private int userLocaleNum;

        public ObservableCollection<EventModel> GlobalEventsList { get; set; } = new ObservableCollection<EventModel>();
        public ObservableCollection<AttendeeModel> ActiveAttendeesList { get; set; } = new ObservableCollection<AttendeeModel>();

        private string activeTabMode = "MyEvents";
        private int selectedEventIdForAction;

        public O_EVENTS(int sessionAccountNum)
        {
            InitializeComponent();
            this.currentAccountNum = sessionAccountNum;
            EventsCollectionView.ItemsSource = GlobalEventsList;
            AttendeesListView.ItemsSource = ActiveAttendeesList;

            // Populate Categories
            LoadCategories();
        }

        private void LoadCategories()
        {
            var list = new List<CategoryModel>
            {
                new CategoryModel { CategoryNum = 1, CategoryName = "Medical Mission" },
                new CategoryModel { CategoryNum = 2, CategoryName = "Feeding Program" },
                new CategoryModel { CategoryNum = 3, CategoryName = "Youth Activity" },
                new CategoryModel { CategoryNum = 4, CategoryName = "Spiritual Gathering" },
                new CategoryModel { CategoryNum = 5, CategoryName = "Environmental Care" }
            };
            PickerCategory.ItemsSource = list;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await SetOrganizerIdentityHeader();
            await SyncEventRegistryDataset();
        }

        private async Task SetOrganizerIdentityHeader()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT Firstname, Lastname, Email, LocaleNum FROM ACCOUNT WHERE AccountNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", currentAccountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                LblHeaderOrganizerName.Text = $"{reader["Firstname"]}!";
                                EntryFormOrganizer.Text = $"{reader["Firstname"]} {reader["Lastname"]}";
                                EntryFormEmail.Text = reader["Email"].ToString();
                                userLocaleNum = Convert.ToInt32(reader["LocaleNum"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private async Task SyncEventRegistryDataset()
        {
            try
            {
                GlobalEventsList.Clear();
                string searchQuery = EventSearchBar.Text ?? "";

                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string baseSql = @"SELECT e.*, a.Firstname + ' ' + a.Lastname AS OrganizerName, l.LocaleName, l.LocaleAddress
                                       FROM EVENT e 
                                       INNER JOIN ACCOUNT a ON e.OrganizerNum = a.AccountNum 
                                       INNER JOIN LOCALE l ON e.LocaleNum = l.LocaleNum
                                       WHERE (e.EventTitle LIKE @Search)";

                    if (activeTabMode == "MyEvents") baseSql += " AND e.OrganizerNum = @UserNum AND e.EventStatus != 'Completed'";
                    else if (activeTabMode == "AllEvents") baseSql += " AND e.EventStatus != 'Completed'";
                    else if (activeTabMode == "Completed") baseSql += " AND e.EventStatus = 'Completed'";

                    using (SqlCommand cmd = new SqlCommand(baseSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Search", $"%{searchQuery}%");
                        cmd.Parameters.AddWithValue("@UserNum", currentAccountNum);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                GlobalEventsList.Add(new EventModel
                                {
                                    EventID = Convert.ToInt32(reader["EventNum"]),
                                    EventTitle = reader["EventTitle"].ToString(),
                                    OrganizerName = reader["OrganizerName"].ToString(),
                                    EventVenue = reader["LocaleName"].ToString(),
                                    EventAddress = reader["LocaleAddress"].ToString(),
                                    EventTime = reader["StartTime"].ToString(),
                                    EventDate = Convert.ToDateTime(reader["EventDate"]).ToString("yyyy-MM-dd"),
                                    EventDetails = reader["EventDescription"].ToString(),
                                    IsMyEvent = (Convert.ToInt32(reader["OrganizerNum"]) == currentAccountNum)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { await DisplayAlert("Database Error", ex.Message, "OK"); }
        }

        private async void PublishNewEventBtn_Clicked(object sender, EventArgs e)
        {
            var selectedCategory = PickerCategory.SelectedItem as CategoryModel;

            // 1. Declare the variable at the top of the method scope
            string eventIdFormatted = string.Empty;

            if (selectedCategory == null || string.IsNullOrWhiteSpace(EntryFormTitle.Text))
            {
                await DisplayAlert("Missing Values", "Title and Category are required.", "OK");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string countSql = "SELECT ISNULL(MAX(EventNum), 0) + 1 FROM EVENT";
                    int nextId = (int)await new SqlCommand(countSql, conn).ExecuteScalarAsync();

                    // 2. Assign the value here
                    eventIdFormatted = "EV" + nextId.ToString("D3");

                    string sql = @"INSERT INTO EVENT 
                          (Event_ID, OrganizerNum, CategoryNum, LocaleNum, EventTitle, EventDescription, 
                           EventDate, StartTime, EndTime, ExpectedParticipants, VolunteerCapacity, EventStatus) 
                           VALUES 
                          (@ID, @Org, @Cat, @Loc, @Title, @Desc, @Date, @Start, @End, 0, @Cap, 'Published')";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", eventIdFormatted);
                        cmd.Parameters.AddWithValue("@Org", currentAccountNum);
                        cmd.Parameters.AddWithValue("@Cat", selectedCategory.CategoryNum);
                        cmd.Parameters.AddWithValue("@Loc", userLocaleNum);
                        cmd.Parameters.AddWithValue("@Title", EntryFormTitle.Text);
                        cmd.Parameters.AddWithValue("@Desc", EditorFormDetails.Text ?? "");
                        cmd.Parameters.AddWithValue("@Date", PickerFormDate.Date);
                        cmd.Parameters.AddWithValue("@Start", PickerStartTime.Time);
                        cmd.Parameters.AddWithValue("@End", PickerEndTime.Time);
                        cmd.Parameters.AddWithValue("@Cap", int.TryParse(EntryFormCapacity.Text, out int cap) ? cap : 0);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                PopupOrganizePanel.IsVisible = false;
                await SyncEventRegistryDataset();
                // 3. Now this works perfectly because the variable is in scope
                await DisplayAlert("Success", $"Event {eventIdFormatted} published!", "OK");
            }
            catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        }

        // --- NAVIGATION & TABS ---
        private async void MyEventsTab_Tapped(object sender, EventArgs e) { UpdateTabVisuals(TabMyEvents, TabAllEvents, TabCompleted); activeTabMode = "MyEvents"; await SyncEventRegistryDataset(); }
        private async void AllEventsTab_Tapped(object sender, EventArgs e) { UpdateTabVisuals(TabAllEvents, TabMyEvents, TabCompleted); activeTabMode = "AllEvents"; await SyncEventRegistryDataset(); }
        private async void CompletedTab_Tapped(object sender, EventArgs e) { UpdateTabVisuals(TabCompleted, TabMyEvents, TabAllEvents); activeTabMode = "Completed"; await SyncEventRegistryDataset(); }
        private void UpdateTabVisuals(Border active, Border inactive1, Border inactive2) { active.BackgroundColor = Colors.White; inactive1.BackgroundColor = Colors.Transparent; inactive2.BackgroundColor = Colors.Transparent; }
        private async void EventSearchBar_TextChanged(object sender, TextChangedEventArgs e) { await SyncEventRegistryDataset(); }
        private void OpenOrganizePanelBtn_Clicked(object sender, EventArgs e) => PopupOrganizePanel.IsVisible = true;
        private void CloseOrganizePanelBtn_Clicked(object sender, EventArgs e) => PopupOrganizePanel.IsVisible = false;
        private void CloseDetailsPanelBtn_Clicked(object sender, EventArgs e) => PopupDetailsPanel.IsVisible = false;
        private async void ViewEventDetailsBtn_Clicked(object sender, EventArgs e) { /* ... keep your existing logic ... */ }
        private async void DeleteEventBtn_Clicked(object sender, EventArgs e) { /* ... keep your existing logic ... */ }
        private async void ConfirmAttendanceBtn_Clicked(object sender, EventArgs e) => await DisplayAlert("Attendance", "Attendance logged.", "OK");
        private async void ConfirmCompletionBtn_Clicked(object sender, EventArgs e) { PopupDetailsPanel.IsVisible = false; await SyncEventRegistryDataset(); }
        private async void BackBtn_Clicked(object sender, EventArgs e) => await Navigation.PopAsync();
        private async void HomeBtn_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new O_HOME(currentAccountNum));
        private async void ProposalsBtn_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new O_PROPOSALS(currentAccountNum));
        private async void ProfileBtn_Clicked(object sender, EventArgs e) => await Navigation.PushAsync(new O_PROFILE(currentAccountNum));

        // --- MODELS ---
        public class EventModel { public int EventID { get; set; } public string EventTitle { get; set; } public string OrganizerName { get; set; } public string EventVenue { get; set; } public string EventAddress { get; set; } public string EventTime { get; set; } public string EventDate { get; set; } public string EventDetails { get; set; } public bool IsMyEvent { get; set; } }
        public class AttendeeModel { public string VolunteerName { get; set; } public string StatusText { get; set; } public Color StatusColor { get; set; } }
        public class CategoryModel { public int CategoryNum { get; set; } public string CategoryName { get; set; } }
    }
}