using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Windows.Input;
using Microsoft.Maui.Graphics;

namespace Handog_MobileApp.ViewModels.Organizer
{
    // --- 1. THE DATA MODEL ---
    public class AppNotification : INotifyPropertyChanged
    {
        public int NotificationID { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set
            {
                if (_isRead != value)
                {
                    _isRead = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BgColor));
                    OnPropertyChanged(nameof(TitleFont));
                }
            }
        }

        // UI Helpers bound to IsRead
        public Color BgColor => IsRead ? Colors.White : Color.FromArgb("#F4F6F8");
        public FontAttributes TitleFont => IsRead ? FontAttributes.None : FontAttributes.Bold;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // --- 2. THE VIEW MODEL ---
    public class OrganizerHomeViewModel : INotifyPropertyChanged
    {
        private int _currentAccountNum;
        private INavigation _navigation;

        // --- Data Binding Properties ---
        private string _organizerName = "Loading...";
        public string OrganizerName
        {
            get => _organizerName;
            set { _organizerName = value; OnPropertyChanged(); }
        }

        private string _myEventsCount = "0";
        public string MyEventsCount
        {
            get => _myEventsCount;
            set { _myEventsCount = value; OnPropertyChanged(); }
        }

        private string _totalEventsText = "out of 0 platform events";
        public string TotalEventsText
        {
            get => _totalEventsText;
            set { _totalEventsText = value; OnPropertyChanged(); }
        }

        private bool _hasUnreadNotifications;
        public bool HasUnreadNotifications
        {
            get => _hasUnreadNotifications;
            set { _hasUnreadNotifications = value; OnPropertyChanged(); }
        }

        // --- Notification Panel Properties ---
        public ObservableCollection<AppNotification> Notifications { get; set; } = new();

        private bool _isNotificationPanelVisible;
        public bool IsNotificationPanelVisible
        {
            get => _isNotificationPanelVisible;
            set { _isNotificationPanelVisible = value; OnPropertyChanged(); }
        }

        // --- Commands ---
        public ICommand OrganizeEventCommand { get; }
        public ICommand NavigateProposalsCommand { get; }
        public ICommand NavigateEventsCommand { get; }
        public ICommand NavigateProfileCommand { get; }
        public ICommand ViewNotificationsCommand { get; }
        public ICommand NotificationTappedCommand { get; }

        public OrganizerHomeViewModel(int accountNum, INavigation navigation)
        {
            _currentAccountNum = accountNum;
            _navigation = navigation;

            // Navigation Commands
            OrganizeEventCommand = new Command(async () => await _navigation.PushAsync(new Views.Organizer.O_EVENTS(_currentAccountNum)));
            NavigateProposalsCommand = new Command(async () => await _navigation.PushAsync(new Views.Organizer.O_PROPOSALS(_currentAccountNum)));
            NavigateEventsCommand = new Command(async () => await _navigation.PushAsync(new Views.Organizer.O_EVENTS(_currentAccountNum)));
            NavigateProfileCommand = new Command(async () => await _navigation.PushAsync(new Views.Organizer.O_PROFILE(_currentAccountNum)));

            // Toggle Notification Panel
            ViewNotificationsCommand = new Command(() =>
            {
                IsNotificationPanelVisible = !IsNotificationPanelVisible;
            });

            // Mark specific notification as read
            NotificationTappedCommand = new Command<AppNotification>(async (notif) => await MarkNotificationAsReadAsync(notif));
        }

        // --- Database Logic ---
        // --- Database Logic ---
        public async Task LoadDashboardDataAsync()
        {
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Connection Error: {ex.Message}");
                    OrganizerName = "Connection Error";
                    return; // Stop if we can't even connect to the DB
                }

                // 1. Get Name (Isolated)
                try
                {
                    string nameQuery = "SELECT Firstname FROM ACCOUNT WHERE AccountNum = @AccountNum";
                    using (SqlCommand cmd = new SqlCommand(nameQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountNum", _currentAccountNum);
                        var result = await cmd.ExecuteScalarAsync();
                        OrganizerName = (result != null && result != DBNull.Value) ? result.ToString() : "Organizer";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading name: {ex.Message}");
                    OrganizerName = "Organizer"; // Fallback name
                }

                // 2. Get Contribution Metrics (Isolated)
                try
                {
                    // FIXED: Changed 'AccountNum' to 'OrganizerNum' for the EVENT table query
                    string metricsQuery = @"
                        SELECT 
                            (SELECT COUNT(*) FROM EVENT WHERE OrganizerNum = @AccountNum) AS MyEvents,
                            (SELECT COUNT(*) FROM EVENT) AS TotalEvents";

                    using (SqlCommand cmd = new SqlCommand(metricsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountNum", _currentAccountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string myEvents = reader["MyEvents"] != DBNull.Value ? reader["MyEvents"].ToString() : "0";
                                string totalEvents = reader["TotalEvents"] != DBNull.Value ? reader["TotalEvents"].ToString() : "0";

                                MyEventsCount = myEvents;
                                TotalEventsText = $"out of {totalEvents} total platform events";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading metrics: {ex.Message}");
                    MyEventsCount = "0";
                    TotalEventsText = "metrics unavailable";
                }

                // 3. Fetch Top 10 Notifications (Isolated)
                try
                {
                    string notifQuery = @"
                        SELECT TOP 10 NotificationID, Title, Message, IsRead 
                        FROM NOTIFICATION 
                        WHERE AccountNum = @AccountNum 
                        ORDER BY CreatedAt DESC";

                    using (SqlCommand cmd = new SqlCommand(notifQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountNum", _currentAccountNum);
                        int unreadCount = 0;

                        // Clear on main thread to avoid UI crash
                        MainThread.BeginInvokeOnMainThread(() => Notifications.Clear());

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bool isRead = Convert.ToBoolean(reader["IsRead"]);
                                if (!isRead) unreadCount++;

                                var notification = new AppNotification
                                {
                                    NotificationID = Convert.ToInt32(reader["NotificationID"]),
                                    Title = reader["Title"].ToString(),
                                    Message = reader["Message"].ToString(),
                                    IsRead = isRead
                                };

                                MainThread.BeginInvokeOnMainThread(() => Notifications.Add(notification));
                            }
                        }
                        HasUnreadNotifications = unreadCount > 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading notifications: {ex.Message}");
                }
            }
        }

        private async Task MarkNotificationAsReadAsync(AppNotification notif)
        {
            if (notif == null || notif.IsRead) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string query = "UPDATE NOTIFICATION SET IsRead = 1 WHERE NotificationID = @ID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", notif.NotificationID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Update UI State locally
                notif.IsRead = true;

                // Recalculate unread badge
                int unreadCount = 0;
                foreach (var n in Notifications) { if (!n.IsRead) unreadCount++; }
                HasUnreadNotifications = unreadCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking read: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}