using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Data.SqlClient;
using Handog_MobileApp.Models;
using Handog_MobileApp.Views.Organizer;

namespace Handog_MobileApp
{
    public class EventsViewModel : BaseViewModel
    {
        private readonly int _currentAccountNum;
        private int _userLocaleNum;
        private string _activeTabMode = "MyEvents";

        // --- UI State Properties ---
        private int _linkedProposalNum = 0;

        // EventNum currently being edited/resubmitted. 0 means we are creating a brand new event.
        private int _editingEventNum = 0;

        private string _organizerNameHeader;
        public string OrganizerNameHeader { get => _organizerNameHeader; set { _organizerNameHeader = value; OnPropertyChanged(); } }

        private string _formOrganizer;
        public string FormOrganizer { get => _formOrganizer; set { _formOrganizer = value; OnPropertyChanged(); } }

        private string _formEmail;
        public string FormEmail { get => _formEmail; set { _formEmail = value; OnPropertyChanged(); } }

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set { _searchQuery = value; OnPropertyChanged(); SyncEventRegistryDataset(); }
        }

        // --- Form Fields for Organizing ---
        private string _formTitle;
        public string FormTitle { get => _formTitle; set { _formTitle = value; OnPropertyChanged(); } }

        private string _formDetails;
        public string FormDetails { get => _formDetails; set { _formDetails = value; OnPropertyChanged(); } }

        private DateTime _formDate = DateTime.Today;
        public DateTime FormDate { get => _formDate; set { _formDate = value; OnPropertyChanged(); } }

        private TimeSpan _formStartTime;
        public TimeSpan FormStartTime { get => _formStartTime; set { _formStartTime = value; OnPropertyChanged(); } }

        private TimeSpan _formEndTime;
        public TimeSpan FormEndTime { get => _formEndTime; set { _formEndTime = value; OnPropertyChanged(); } }

        private string _formCapacity;
        public string FormCapacity { get => _formCapacity; set { _formCapacity = value; OnPropertyChanged(); } }

        private CategoryModel _selectedCategory;
        public CategoryModel SelectedCategory { get => _selectedCategory; set { _selectedCategory = value; OnPropertyChanged(); } }

        // --- Toggles & Tabs ---
        private bool _isOrganizePanelVisible;
        public bool IsOrganizePanelVisible { get => _isOrganizePanelVisible; set { _isOrganizePanelVisible = value; OnPropertyChanged(); } }

        private Color _myEventsTabColor = Colors.White;
        public Color MyEventsTabColor { get => _myEventsTabColor; set { _myEventsTabColor = value; OnPropertyChanged(); } }

        private Color _allEventsTabColor = Colors.Transparent;
        public Color AllEventsTabColor { get => _allEventsTabColor; set { _allEventsTabColor = value; OnPropertyChanged(); } }

        private Color _completedTabColor = Colors.Transparent;
        public Color CompletedTabColor { get => _completedTabColor; set { _completedTabColor = value; OnPropertyChanged(); } }

        // Add this property to your EventsViewModel.cs
        public EventModel CurrentEvent { get; set; }

        // ---- Address ---- 

        // --- Form Fields for Organizing ---
        // (Add these next to FormTitle, FormDetails, etc.)
        private string _formVenue;
        public string FormVenue { get => _formVenue; set { _formVenue = value; OnPropertyChanged(); } }

        private string _formAddress;
        public string FormAddress { get => _formAddress; set { _formAddress = value; OnPropertyChanged(); } }

        // --- Collections ---
        public ObservableCollection<EventModel> GlobalEventsList { get; set; } = new ObservableCollection<EventModel>();
        public List<CategoryModel> CategoriesList { get; set; }

        // --- Commands ---
        public ICommand SwitchTabCommand { get; }
        public ICommand PublishEventCommand { get; }
        public ICommand TogglePanelCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand ViewEventDetailsCommand { get; }
        public ICommand DeleteEventCommand { get; }
        public ICommand EditEventCommand { get; }

        public EventsViewModel(int sessionAccountNum)
        {
            _currentAccountNum = sessionAccountNum;
            LoadCategories();

            SwitchTabCommand = new Command<string>(async (tab) => await ExecuteSwitchTab(tab));
            PublishEventCommand = new Command(async () => await ExecutePublishNewEvent());
            TogglePanelCommand = new Command<string>((panel) => ExecuteTogglePanel(panel));

            BackCommand = new Command(async () => await Application.Current.MainPage.Navigation.PopAsync());
            NavigateCommand = new Command<string>(async (dest) => await ExecuteNavigation(dest));

            // This catches the exact EventModel the user clicks in the UI and sends it to the Details page
            // Replace your existing ViewEventDetailsCommand assignment with this:
            ViewEventDetailsCommand = new Command<EventModel>(async (selectedEvent) =>
            {
                if (selectedEvent == null) return;

                // Smart Routing: Check the status before navigating!
                if (selectedEvent.EventStatus == "Completed")
                {
                    // Route to the new Reports page
                    await Application.Current.MainPage.Navigation.PushAsync(new O_REPORT(selectedEvent));
                }
                else
                {
                    // Route to the normal Details page
                    await Application.Current.MainPage.Navigation.PushAsync(new O_EVENT_DETAILS(selectedEvent));
                }
            });

            DeleteEventCommand = new Command<EventModel>(async (evt) => await ExecuteDeleteEvent(evt));
            EditEventCommand = new Command<EventModel>(async (evt) => await ExecuteEditEvent(evt));
        }
        public void LoadProposalIntoForm(O_EventProposal proposal)
        {
            if (proposal == null) return;

            _linkedProposalNum = proposal.ProposalNum;
            FormTitle = proposal.ProposalTitle;

            // REMOVED: FormDetails = proposal.ProposalDetails;
            // By leaving FormDetails untouched, the Event Details text box will stay empty 
            // for the organizer to write their own description.

            FormDate = proposal.PreferredDate;
            FormStartTime = proposal.PreferredStartTime;
            FormEndTime = proposal.PreferredEndTime;

            SelectedCategory = CategoriesList.FirstOrDefault(c => c.CategoryNum == proposal.CategoryNum);
            IsOrganizePanelVisible = true;
        }

        public async Task InitializeAsync()
        {
            await SetOrganizerIdentityHeader();
            await SyncEventRegistryDataset();
        }

        private void LoadCategories()
        {
            CategoriesList = new List<CategoryModel>
            {
                new CategoryModel { CategoryNum = 1, CategoryName = "Medical Mission" },
                new CategoryModel { CategoryNum = 2, CategoryName = "Feeding Program" },
                new CategoryModel { CategoryNum = 3, CategoryName = "Youth Activity" },
                new CategoryModel { CategoryNum = 4, CategoryName = "Spiritual Gathering" },
                new CategoryModel { CategoryNum = 5, CategoryName = "Environmental Care" }
            };
        }

        private async Task SetOrganizerIdentityHeader()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    // JOIN the LOCALE table so we can grab the actual text of their default location
                    string query = @"SELECT a.Firstname, a.Lastname, a.Email, a.LocaleNum, 
                                    l.LocaleName, l.LocaleAddress 
                             FROM ACCOUNT a
                             LEFT JOIN LOCALE l ON a.LocaleNum = l.LocaleNum
                             WHERE a.AccountNum = @AccNum";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", _currentAccountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                OrganizerNameHeader = $"{reader["Firstname"]}!";
                                FormOrganizer = $"{reader["Firstname"]} {reader["Lastname"]}";
                                FormEmail = reader["Email"].ToString();
                                _userLocaleNum = reader["LocaleNum"] != DBNull.Value ? Convert.ToInt32(reader["LocaleNum"]) : 0;

                                // Preload the form fields! (The user can still delete/edit this text in the app)
                                FormVenue = reader["LocaleName"]?.ToString();
                                FormAddress = reader["LocaleAddress"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        public async Task SyncEventRegistryDataset()
        {
            try
            {
                GlobalEventsList.Clear();
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    // FIX 1: Removed the LOCALE join. e.* already grabs EventVenue and EventAddress!
                    string baseSql = @"SELECT e.*, a.Firstname + ' ' + a.Lastname AS OrganizerName
                               FROM EVENT e 
                               INNER JOIN ACCOUNT a ON e.OrganizerNum = a.AccountNum 
                               WHERE (e.EventTitle LIKE @Search)";

                    if (_activeTabMode == "MyEvents") baseSql += " AND e.OrganizerNum = @UserNum AND e.EventStatus != 'Completed'";
                    else if (_activeTabMode == "AllEvents") baseSql += " AND e.EventStatus != 'Completed'";
                    else if (_activeTabMode == "Completed") baseSql += " AND e.EventStatus = 'Completed'";

                    using (SqlCommand cmd = new SqlCommand(baseSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Search", $"%{SearchQuery}%");
                        cmd.Parameters.AddWithValue("@UserNum", _currentAccountNum);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int catNum = reader["CategoryNum"] != DBNull.Value ? Convert.ToInt32(reader["CategoryNum"]) : 1;

                                // FIX 2: Added a default case to the switch expression so the compiler is happy
                                string imagePlaceholder = catNum switch
                                {
                                    1 => "mm.png",  // Medical Mission
                                    2 => "fp.png",  // Feeding Program
                                    3 => "ya.png",  // Youth Activity
                                    4 => "sg.png",  // Spiritual Gathering
                                    5 => "tp.png",  // Environmental Care
                                    _ => "mm.png"   // Fallback image just in case
                                };

                                GlobalEventsList.Add(new EventModel
                                {
                                    EventID = Convert.ToInt32(reader["EventNum"]),
                                    EventTitle = reader["EventTitle"].ToString(),
                                    OrganizerName = reader["OrganizerName"].ToString(),

                                    // FIX 3: Read directly from your newly added columns in the EVENT table
                                    EventVenue = reader["EventVenue"]?.ToString(),
                                    EventAddress = reader["EventAddress"]?.ToString(),

                                    EventTime = reader["StartTime"].ToString(),
                                    EventDate = Convert.ToDateTime(reader["EventDate"]).ToString("yyyy-MM-dd"),
                                    EventDetails = reader["EventDescription"].ToString(),
                                    IsMyEvent = (Convert.ToInt32(reader["OrganizerNum"]) == _currentAccountNum),

                                    EventStatus = reader["EventStatus"]?.ToString(),
                                    RejectionReason = reader["RejectionReason"] != DBNull.Value ? reader["RejectionReason"].ToString() : null,

                                    CategoryImage = imagePlaceholder
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Database Error", ex.Message, "OK"); }
        }

        private async Task ExecutePublishNewEvent()
        {
            // Updated validation to check for Venue instead of SelectedLocale
            if (SelectedCategory == null || string.IsNullOrWhiteSpace(FormTitle) || string.IsNullOrWhiteSpace(FormVenue))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Title, Category, and Venue Name are required.", "OK");
                return;
            }

            // Editing a rejected event takes a different (UPDATE) path than creating a new one.
            if (_editingEventNum > 0)
            {
                await ExecuteResubmitEvent();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    string countSql = "SELECT ISNULL(MAX(EventNum), 0) + 1 FROM EVENT";
                    int nextId = (int)await new SqlCommand(countSql, conn).ExecuteScalarAsync();
                    string eventIdFormatted = "EV" + nextId.ToString("D3");

                    // New events start as 'Pending' and must be approved by an admin before going live.
                    string sql = @"INSERT INTO EVENT (Event_ID, OrganizerNum, CategoryNum, LocaleNum, EventTitle, EventDescription, EventDate, StartTime, EndTime, ExpectedParticipants, VolunteerCapacity, EventStatus, EventVenue, EventAddress)
                           VALUES (@ID, @Org, @Cat, @Loc, @Title, @Desc, @Date, @Start, @End, 0, @Cap, 'Pending', @Venue, @Address)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", eventIdFormatted);
                        cmd.Parameters.AddWithValue("@Org", _currentAccountNum);
                        cmd.Parameters.AddWithValue("@Cat", SelectedCategory.CategoryNum);
                        cmd.Parameters.AddWithValue("@Loc", _userLocaleNum); // Keeps the original binding for analytics if you still want it

                        cmd.Parameters.AddWithValue("@Title", FormTitle);
                        cmd.Parameters.AddWithValue("@Desc", FormDetails ?? "");
                        cmd.Parameters.AddWithValue("@Date", FormDate);
                        cmd.Parameters.AddWithValue("@Start", FormStartTime);
                        cmd.Parameters.AddWithValue("@End", FormEndTime);
                        cmd.Parameters.AddWithValue("@Cap", int.TryParse(FormCapacity, out int cap) ? cap : 0);

                        // Add the new text fields
                        cmd.Parameters.AddWithValue("@Venue", FormVenue);
                        cmd.Parameters.AddWithValue("@Address", FormAddress ?? "");

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 2. Link & Update Proposal Status
                    if (_linkedProposalNum > 0)
                    {
                        string updateProposal = "UPDATE EVENTPROPOSAL SET ProposalStatus = 'Approved' WHERE ProposalNum = @PNum";
                        using var cmdUpd = new SqlCommand(updateProposal, conn);
                        cmdUpd.Parameters.AddWithValue("@PNum", _linkedProposalNum);
                        await cmdUpd.ExecuteNonQueryAsync();
                    }
                }

                _linkedProposalNum = 0; // Reset
                ClearForm();
                IsOrganizePanelVisible = false;
                await SyncEventRegistryDataset();
                await Application.Current.MainPage.DisplayAlert("Success", "Event submitted for admin approval.", "OK");
            }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"); }
        }

        // Loads a previously-rejected event back into the Organize panel so the organizer can amend and resubmit it.
        private async Task ExecuteEditEvent(EventModel evt)
        {
            if (evt == null || !evt.CanEdit) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"SELECT CategoryNum, EventTitle, EventDescription, EventDate, StartTime, EndTime, VolunteerCapacity, EventVenue, EventAddress
                                   FROM EVENT WHERE EventNum = @Num";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Num", evt.EventID);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int catNum = reader["CategoryNum"] != DBNull.Value ? Convert.ToInt32(reader["CategoryNum"]) : 0;
                                SelectedCategory = CategoriesList.FirstOrDefault(c => c.CategoryNum == catNum);

                                FormTitle = reader["EventTitle"]?.ToString();
                                FormDetails = reader["EventDescription"] != DBNull.Value ? reader["EventDescription"].ToString() : string.Empty;
                                FormDate = reader["EventDate"] != DBNull.Value ? Convert.ToDateTime(reader["EventDate"]) : DateTime.Today;
                                FormStartTime = reader["StartTime"] != DBNull.Value ? (TimeSpan)reader["StartTime"] : TimeSpan.Zero;
                                FormEndTime = reader["EndTime"] != DBNull.Value ? (TimeSpan)reader["EndTime"] : TimeSpan.Zero;
                                FormCapacity = reader["VolunteerCapacity"] != DBNull.Value ? reader["VolunteerCapacity"].ToString() : "0";
                                FormVenue = reader["EventVenue"]?.ToString();
                                FormAddress = reader["EventAddress"] != DBNull.Value ? reader["EventAddress"].ToString() : string.Empty;
                            }
                        }
                    }
                }

                _editingEventNum = evt.EventID;
                IsOrganizePanelVisible = true;
            }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"); }
        }

        // Saves edits to a rejected event and sends it back to 'Pending' for another admin review.
        private async Task ExecuteResubmitEvent()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"UPDATE EVENT
                                   SET CategoryNum = @Cat, EventTitle = @Title, EventDescription = @Desc,
                                       EventDate = @Date, StartTime = @Start, EndTime = @End,
                                       VolunteerCapacity = @Cap, EventVenue = @Venue, EventAddress = @Address,
                                       EventStatus = 'Pending', RejectionReason = NULL
                                   WHERE EventNum = @Num";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Cat", SelectedCategory.CategoryNum);
                        cmd.Parameters.AddWithValue("@Title", FormTitle);
                        cmd.Parameters.AddWithValue("@Desc", FormDetails ?? "");
                        cmd.Parameters.AddWithValue("@Date", FormDate);
                        cmd.Parameters.AddWithValue("@Start", FormStartTime);
                        cmd.Parameters.AddWithValue("@End", FormEndTime);
                        cmd.Parameters.AddWithValue("@Cap", int.TryParse(FormCapacity, out int cap) ? cap : 0);
                        cmd.Parameters.AddWithValue("@Venue", FormVenue);
                        cmd.Parameters.AddWithValue("@Address", FormAddress ?? "");
                        cmd.Parameters.AddWithValue("@Num", _editingEventNum);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                _editingEventNum = 0;
                ClearForm();
                IsOrganizePanelVisible = false;
                await SyncEventRegistryDataset();
                await Application.Current.MainPage.DisplayAlert("Success", "Event resubmitted for admin approval.", "OK");
            }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"); }
        }

        // Resets the Organize panel fields (keeps the preloaded organizer identity intact).
        private void ClearForm()
        {
            _editingEventNum = 0;
            FormTitle = string.Empty;
            FormDetails = string.Empty;
            FormCapacity = string.Empty;
            FormDate = DateTime.Today;
            FormStartTime = TimeSpan.Zero;
            FormEndTime = TimeSpan.Zero;
            SelectedCategory = null;
        }

        private async Task ExecuteSwitchTab(string tab)
        {
            _activeTabMode = tab;
            MyEventsTabColor = tab == "MyEvents" ? Colors.White : Colors.Transparent;
            AllEventsTabColor = tab == "AllEvents" ? Colors.White : Colors.Transparent;
            CompletedTabColor = tab == "Completed" ? Colors.White : Colors.Transparent;
            await SyncEventRegistryDataset();
        }

        private async Task ExecuteDeleteEvent(EventModel evt)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Delete Event", $"Are you sure you want to delete '{evt.EventTitle}'?", "Yes", "No");
            if (!confirm) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = "DELETE FROM EVENT WHERE EventNum = @EvtNum";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@EvtNum", evt.EventID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await SyncEventRegistryDataset(); // Refresh the list
            }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"); }
        }

        private void ExecuteTogglePanel(string action)
        {
            if (action == "OpenOrganize")
            {
                // "+ ORGANIZE" always opens a fresh create form, never an in-progress edit.
                ClearForm();
                IsOrganizePanelVisible = true;
            }
            else if (action == "CloseOrganize")
            {
                _editingEventNum = 0;
                IsOrganizePanelVisible = false;
            }
        }

        private async Task ExecuteNavigation(string dest)
        {
            // Update these page names if they differ slightly in your actual project
            Page targetPage = dest switch
            {
                "Home" => new O_HOME(_currentAccountNum),
                "Proposals" => new O_PROPOSALS(_currentAccountNum),
                "Profile" => new O_PROFILE(_currentAccountNum),
                _ => null
            };
            if (targetPage != null) await Application.Current.MainPage.Navigation.PushAsync(targetPage);
        }
    }
}

