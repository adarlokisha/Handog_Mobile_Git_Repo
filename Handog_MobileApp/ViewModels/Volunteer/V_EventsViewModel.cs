using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Data.SqlClient;
using Handog_MobileApp.Models;
using System.Linq;
using Handog_MobileApp.Views.Volunteer;

namespace Handog_MobileApp.ViewModels.Volunteer
{
    public class V_EventsViewModel : INotifyPropertyChanged
    {
        // Commands
        public ICommand FilterCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand UnregisterCommand { get; }
        public ICommand ViewEventDetailsCommand { get; }
        public ICommand CloseDetailsCommand { get; }
        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToEventsCommand { get; }
        public ICommand NavigateToProposalsCommand { get; }
        public ICommand NavigateToProfileCommand { get; }

        private int _loggedInAccountNum;
        private readonly INavigation _navigation;
        private string _activeTabMode = "AllEvents"; // Default view tracking

        public event Action<string, string> ShowAlertRequested;
        public ObservableCollection<EventModel> DisplayedEvents { get; } = new();

        // --- Layout Screen View States ---
        private string _volunteerNameHeader;
        // Basis derived natively from the organizer profile header assignment rules
        public string VolunteerNameHeader { get => _volunteerNameHeader; set { _volunteerNameHeader = value; OnPropertyChanged(); } }

        private bool _isListViewVisible = true;
        public bool IsListViewVisible
        {
            get => _isListViewVisible;
            set { _isListViewVisible = value; OnPropertyChanged(); }
        }

        private bool _isDetailsViewVisible = false;
        public bool IsDetailsViewVisible
        {
            get => _isDetailsViewVisible;
            set { _isDetailsViewVisible = value; OnPropertyChanged(); }
        }

        private EventModel _selectedEventTarget;
        public EventModel SelectedEventTarget
        {
            get => _selectedEventTarget;
            set { _selectedEventTarget = value; OnPropertyChanged(); }
        }

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged();
                    _ = LoadEventsFromDatabaseAsync(); // Trigger live filter
                }
            }
        }

        public V_EventsViewModel(INavigation navigation, int accountNum)
        {
            _navigation = navigation;
            _loggedInAccountNum = accountNum;

            FilterCommand = new Command<string>(async (filterType) => await ExecuteFilterAsync(filterType));
            RegisterCommand = new Command<EventModel>(async (ev) => await ExecuteRegisterAsync(ev));
            UnregisterCommand = new Command<EventModel>(async (ev) => await ExecuteUnregisterAsync(ev));

            // View Toggles Layout Management
            ViewEventDetailsCommand = new Command<EventModel>(ExecuteViewDetails);
            CloseDetailsCommand = new Command(ExecuteCloseDetails);

            NavigateToHomeCommand = new Command<object>(async (btn) => await ExecuteNavigateToHome(btn));
            NavigateToEventsCommand = new Command<object>(async (btn) => await ExecuteNavigateToEvents(btn));
            NavigateToProposalsCommand = new Command<object>(async (btn) => await ExecuteNavigateToProposals(btn));
            NavigateToProfileCommand = new Command<object>(async (btn) => await ExecuteNavigateToProfile(btn));
        }

        public void SetUserSession(int accountNum)
        {
            _loggedInAccountNum = accountNum;
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            await SetVolunteerIdentityHeader();
            await LoadEventsFromDatabaseAsync();
        }

        private async Task SetVolunteerIdentityHeader()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string query = @"SELECT Firstname FROM ACCOUNT WHERE AccountNum = @AccNum";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", _loggedInAccountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                VolunteerNameHeader = $"{reader["Firstname"]}!";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Identity Header Error: {ex.Message}");
            }
        }

        private async Task ExecuteFilterAsync(string filterType)
        {
            _activeTabMode = filterType;
            ExecuteCloseDetails(); // Close current view panel back to list when changing tabs
            await LoadEventsFromDatabaseAsync();
        }

        public async Task LoadEventsFromDatabaseAsync()
        {
            try
            {
                DisplayedEvents.Clear();
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. Modified query to detect registration status dynamically per row using a LEFT JOIN
                    string query = @"SELECT e.*, 
                                    a.Firstname + ' ' + a.Lastname AS OrganizerName,
                                    CASE WHEN er.Registration_ID IS NOT NULL THEN 1 ELSE 0 END AS IsRegistered
                             FROM EVENT e
                             INNER JOIN ACCOUNT a ON e.OrganizerNum = a.AccountNum
                             LEFT JOIN EVENTREGISTRATION er ON e.EventNum = er.EventNum AND er.AccountNum = @UserNum
                             WHERE e.EventStatus = 'Published'";

                    if (!string.IsNullOrWhiteSpace(SearchQuery))
                    {
                        query += " AND e.EventTitle LIKE @Search";
                    }

                    if (_activeTabMode == "MyEvents")
                    {
                        query += " AND er.Registration_ID IS NOT NULL";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Always add UserNum parameter since it's needed for the LEFT JOIN status check
                        cmd.Parameters.AddWithValue("@UserNum", _loggedInAccountNum);

                        if (!string.IsNullOrWhiteSpace(SearchQuery))
                            cmd.Parameters.AddWithValue("@Search", $"%{SearchQuery}%");

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Inside your SqlDataReader while loop:
                                int currentEventId = Convert.ToInt32(reader["EventNum"]);

                                // Force IsMyEvent to False when on All Events tab so XAML DataTrigger shows the "REGISTER" button
                                bool registeredState = false;

                                if (_activeTabMode == "MyEvents")
                                {
                                    registeredState = true; // Shows "VIEW DETAILS" instead
                                }
                                int catNum = reader["CategoryNum"] != DBNull.Value ? Convert.ToInt32(reader["CategoryNum"]) : 1;
                                string imagePlaceholder = catNum switch
                                {
                                    1 => "mm.png",
                                    2 => "fp.png",
                                    3 => "ya.png",
                                    4 => "sg.png",
                                    5 => "tp.png",
                                    _ => "mm.png"
                                };

                                DisplayedEvents.Add(new EventModel
                                {
                                    EventID = currentEventId,
                                    EventTitle = reader["EventTitle"]?.ToString() ?? "New Event",
                                    EventDetails = reader["EventDescription"]?.ToString() ?? "",
                                    EventVenue = reader["EventVenue"]?.ToString() ?? "",
                                    EventAddress = reader["EventAddress"]?.ToString() ?? "TBD",
                                    OrganizerName = reader["OrganizerName"]?.ToString() ?? "Anonymous",
                                    EventDate = Convert.ToDateTime(reader["EventDate"]).ToString("yyyy-MM-dd"),
                                    EventTime = reader["StartTime"]?.ToString() ?? "",
                                    IsMyEvent = registeredState, // Now evaluates correctly!
                                    CategoryImage = imagePlaceholder
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database Error: {ex.Message}");
                ShowAlertRequested?.Invoke("Error", "Failed to fetch active event dataset.");
            }
        }

        private async Task ExecuteRegisterAsync(EventModel selectedEvent)
        {
            if (selectedEvent == null) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    string checkSql = "SELECT COUNT(*) FROM EVENTREGISTRATION WHERE EventNum = @EvtNum AND AccountNum = @AccNum";
                    using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@EvtNum", selectedEvent.EventID);
                        checkCmd.Parameters.AddWithValue("@AccNum", _loggedInAccountNum);
                        if ((int)await checkCmd.ExecuteScalarAsync() > 0)
                        {
                            ShowAlertRequested?.Invoke("Notice", "You are already registered.");
                            return;
                        }
                    }

                    string shortTimeToken = DateTime.Now.ToString("HHmmss");
                    string regIdFormatted = "R" + shortTimeToken;

                    string registerSql = @"INSERT INTO EVENTREGISTRATION 
                                   (Registration_ID, EventNum, AccountNum, RegistrationDate, RegistrationStatus, AttendanceStatus) 
                                   VALUES 
                                   (@RegID, @EvtNum, @AccNum, GETDATE(), 'Approved', 'Pending')";

                    using (SqlCommand registerCmd = new SqlCommand(registerSql, conn))
                    {
                        registerCmd.Parameters.AddWithValue("@RegID", regIdFormatted);
                        registerCmd.Parameters.AddWithValue("@EvtNum", selectedEvent.EventID);
                        registerCmd.Parameters.AddWithValue("@AccNum", _loggedInAccountNum);
                        await registerCmd.ExecuteNonQueryAsync();
                    }
                }

                ShowAlertRequested?.Invoke("Success", $"Registered for: {selectedEvent.EventTitle}");
                await LoadEventsFromDatabaseAsync();
            }
            catch (Exception ex)
            {
                ShowAlertRequested?.Invoke("Error", $"Registration failed: {ex.Message}");
            }
        }

        private async Task ExecuteUnregisterAsync(EventModel selectedEvent)
        {
            if (selectedEvent == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert("Cancel Registration", $"Unregister from '{selectedEvent.EventTitle}'?", "Yes", "No");
            if (!confirm) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string deleteSql = "DELETE FROM EVENTREGISTRATION WHERE EventNum = @EvtNum AND AccountNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@EvtNum", selectedEvent.EventID);
                        cmd.Parameters.AddWithValue("@AccNum", _loggedInAccountNum);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                ShowAlertRequested?.Invoke("Success", "Registration canceled.");

                if (IsDetailsViewVisible) ExecuteCloseDetails();

                await LoadEventsFromDatabaseAsync();
            }
            catch (Exception ex)
            {
                ShowAlertRequested?.Invoke("Error", ex.Message);
            }
        }

        private void ExecuteViewDetails(EventModel selectedEvent)
        {
            if (selectedEvent == null || !selectedEvent.IsMyEvent) return;

            SelectedEventTarget = selectedEvent;
            IsListViewVisible = false;
            IsDetailsViewVisible = true;
        }

        private void ExecuteCloseDetails()
        {
            IsDetailsViewVisible = false;
            IsListViewVisible = true;
            SelectedEventTarget = null;
        }

        private async Task AnimateButtonAsync(object buttonObj)
        {
            if (buttonObj is ImageButton imgButton)
            {
                await imgButton.ScaleTo(0.92, 50, Easing.Linear);
                await imgButton.ScaleTo(1.0, 50, Easing.Linear);
            }
            else if (buttonObj is Button flatButton)
            {
                await flatButton.ScaleTo(0.92, 50, Easing.Linear);
                await flatButton.ScaleTo(1.0, 50, Easing.Linear);
            }
        }

        private async Task ExecuteNavigateToHome(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_HOME(_loggedInAccountNum));
        }

        private async Task ExecuteNavigateToEvents(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_EVENTS(_loggedInAccountNum));
        }

        private async Task ExecuteNavigateToProposals(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_PROPOSALS(_loggedInAccountNum));
        }

        private async Task ExecuteNavigateToProfile(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_PROFILE(_loggedInAccountNum));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}