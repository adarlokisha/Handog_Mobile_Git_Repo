using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Data.SqlClient;
using Handog_MobileApp.Models;

namespace Handog_MobileApp
{
    public class EventsViewModel : BaseViewModel
    {
        private readonly int _currentAccountNum;
        private int _userLocaleNum;
        private string _activeTabMode = "MyEvents";

        // --- UI State Properties ---
        private int _linkedProposalNum = 0;

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
            ViewEventDetailsCommand = new Command<EventModel>(async (selectedEvent) => await Application.Current.MainPage.Navigation.PushAsync(new O_EVENT_DETAILS(selectedEvent)));

            DeleteEventCommand = new Command<EventModel>(async (evt) => await ExecuteDeleteEvent(evt));
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
                    string query = "SELECT Firstname, Lastname, Email, LocaleNum FROM ACCOUNT WHERE AccountNum = @AccNum";
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
                                _userLocaleNum = Convert.ToInt32(reader["LocaleNum"]);
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
                    string baseSql = @"SELECT e.*, a.Firstname + ' ' + a.Lastname AS OrganizerName, l.LocaleName, l.LocaleAddress
                                       FROM EVENT e 
                                       INNER JOIN ACCOUNT a ON e.OrganizerNum = a.AccountNum 
                                       INNER JOIN LOCALE l ON e.LocaleNum = l.LocaleNum
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
                                    IsMyEvent = (Convert.ToInt32(reader["OrganizerNum"]) == _currentAccountNum)
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
            if (SelectedCategory == null || string.IsNullOrWhiteSpace(FormTitle))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Title and Category are required.", "OK");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. Insert Event
                    string countSql = "SELECT ISNULL(MAX(EventNum), 0) + 1 FROM EVENT";
                    int nextId = (int)await new SqlCommand(countSql, conn).ExecuteScalarAsync();
                    string eventIdFormatted = "EV" + nextId.ToString("D3");

                    string sql = @"INSERT INTO EVENT (Event_ID, OrganizerNum, CategoryNum, LocaleNum, EventTitle, EventDescription, EventDate, StartTime, EndTime, ExpectedParticipants, VolunteerCapacity, EventStatus) 
                               VALUES (@ID, @Org, @Cat, @Loc, @Title, @Desc, @Date, @Start, @End, 0, @Cap, 'Published')";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", eventIdFormatted);
                        cmd.Parameters.AddWithValue("@Org", _currentAccountNum);
                        cmd.Parameters.AddWithValue("@Cat", SelectedCategory.CategoryNum);
                        cmd.Parameters.AddWithValue("@Loc", _userLocaleNum);
                        cmd.Parameters.AddWithValue("@Title", FormTitle);
                        cmd.Parameters.AddWithValue("@Desc", FormDetails ?? "");
                        cmd.Parameters.AddWithValue("@Date", FormDate);
                        cmd.Parameters.AddWithValue("@Start", FormStartTime);
                        cmd.Parameters.AddWithValue("@End", FormEndTime);
                        cmd.Parameters.AddWithValue("@Cap", int.TryParse(FormCapacity, out int cap) ? cap : 0);
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
                IsOrganizePanelVisible = false;
                await SyncEventRegistryDataset();
                await Application.Current.MainPage.DisplayAlert("Success", "Event published!", "OK");
            }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"); }
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
            if (action == "OpenOrganize") IsOrganizePanelVisible = true;
            else if (action == "CloseOrganize") IsOrganizePanelVisible = false;
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