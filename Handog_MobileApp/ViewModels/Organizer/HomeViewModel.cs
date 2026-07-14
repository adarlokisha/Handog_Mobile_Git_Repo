using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using Handog_MobileApp.Services;
using Handog_MobileApp.Models;

namespace Handog_MobileApp.ViewModels.Organizer
{
    // --- 1. THE DATA MODEL ---
    

    // --- 2. THE VIEW MODEL ---
    public class OrganizerHomeViewModel : INotifyPropertyChanged
    {
        private int _currentAccountNum;
        private INavigation _navigation;
        private readonly NotificationService _notifService = new();

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
        public ObservableCollection<NotificationModel> Notifications { get; set; } = new();

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
            NotificationTappedCommand = new Command<NotificationModel>(async (notif) => await MarkNotificationAsReadAsync(notif));
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
                    var data = await _notifService.GetNotificationsAsync(_currentAccountNum);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Notifications.Clear();
                        foreach (var n in data) Notifications.Add(n);
                        HasUnreadNotifications = data.Any(n => !n.IsRead);
                    });
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error Loading Notificatins: {ex.Message}"); }
            }
        }


        private async Task MarkNotificationAsReadAsync(NotificationModel notif)
        {
            if (notif == null || notif.IsRead) return;

            try
            {
                await _notifService.MarkAsReadAsync(notif.NotificationID);
                notif.IsRead = true;
                HasUnreadNotifications = Notifications.Any(n => !n.IsRead);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}