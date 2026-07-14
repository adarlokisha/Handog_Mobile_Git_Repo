using Handog_MobileApp.Services; 
using Handog_MobileApp.Views.Volunteer;
using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Handog_MobileApp.ViewModels.Organizer;
using Handog_MobileApp.Models;

namespace Handog_MobileApp.ViewModels;

public class V_HomeViewModel : INotifyPropertyChanged
{

    // Page navigation commands specific to the Volunteer Hub
    public ICommand NavigateToHomeCommand { get; }
    public ICommand NavigateToEventsCommand { get; }
    public ICommand NavigateToProposalsCommand { get; }
    public ICommand NavigateToProfileCommand { get; }

    public ICommand NotificationTappedCommand { get; }
    public ICommand ViewNotificationsCommand { get; }

    private readonly int _loggedInAccountNum;
    private readonly INavigation _navigation;
    private readonly NotificationService _notifService = new();
    private IDispatcherTimer _timer;

    // Notification properties (replicated for the volunteer dashboard)
    public ObservableCollection<NotificationModel> Notifications { get; set; } = new();

    private bool _hasUnreadNotifications;
    public bool HasUnreadNotifications
    {
        get => _hasUnreadNotifications;
        set { _hasUnreadNotifications = value; OnPropertyChanged(); }
    }

    private bool _isNotificationPanelVisible;
    public bool IsNotificationPanelVisible
    {
        get => _isNotificationPanelVisible;
        set { _isNotificationPanelVisible = value; OnPropertyChanged(); }
    }

    // View dashboard properties
    private string _welcomeName = "Volunteer";
    public string WelcomeName
    {
        get => _welcomeName;
        set { _welcomeName = value; OnPropertyChanged(); }
    }

    private int _joinedEvents;
    public int JoinedEvents
    {
        get => _joinedEvents;
        set { _joinedEvents = value; OnPropertyChanged(); }
    }

    private int _totalEvents;
    public int TotalEvents
    {
        get => _totalEvents;
        set { _totalEvents = value; OnPropertyChanged(); }
    }

    public V_HomeViewModel(INavigation navigation, int accountNum)
    {
        _navigation = navigation;
        _loggedInAccountNum = accountNum;

        //timer that trigger refresh logic
        _timer = Application.Current.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(30);
        _timer.Tick += async (s, e) => await InitializeAsync();
        _timer.Start();

        // Dashboard specific button mappings
        NotificationTappedCommand = new Command<NotificationModel>(async (notif) => await MarkNotificationAsReadAsync(notif));
        ViewNotificationsCommand = new Command(() => IsNotificationPanelVisible = !IsNotificationPanelVisible);

        NavigateToHomeCommand = new Command<object>(async (btn) => await ExecuteNavigateToHome(btn));
        NavigateToEventsCommand = new Command<object>(async (btn) => await ExecuteNavigateToEvents(btn));
        NavigateToProposalsCommand = new Command<object>(async (btn) => await ExecuteNavigateToProposals(btn));
        NavigateToProfileCommand = new Command<object>(async (btn) => await ExecuteNavigateToProfile(btn));
    }
    public void StopTimer()
    {
        _timer?.Stop();
    }

    public async Task InitializeAsync()
    {
        if (_loggedInAccountNum <= 0) return;

        // 1. Fetch Notifications via the Service
        try
        {
            var data = await _notifService.GetNotificationsAsync(_loggedInAccountNum);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Notifications.Clear();
                foreach (var n in data) Notifications.Add(n);
                HasUnreadNotifications = data.Any(n => !n.IsRead);
            });
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Notification Error: {ex.Message}"); }


        // 2. Fetch volunteer metrics from data access layer service
        try
        {
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                await conn.OpenAsync();

                // Query 1: Fetch user's first name
                string nameQuery = "SELECT Firstname FROM ACCOUNT WHERE AccountNum = @AccountNum";
                using (SqlCommand cmdName = new SqlCommand(nameQuery, conn))
                {
                    cmdName.Parameters.AddWithValue("@AccountNum", _loggedInAccountNum);
                    var nameResult = await cmdName.ExecuteScalarAsync();
                    if (nameResult != null)
                    {
                        WelcomeName = nameResult.ToString();
                    }
                }

                // Query 2: Query registration stats matching database context schemas
                string metricsQuery = @"
    SELECT 
        (SELECT COUNT(*) FROM EVENT) AS TotalEvents,
        (SELECT COUNT(*) FROM EVENTREGISTRATION WHERE AccountNum = @AccountNum) AS JoinedEvents";

                using (SqlCommand cmdMetrics = new SqlCommand(metricsQuery, conn))
                {
                    cmdMetrics.Parameters.AddWithValue("@AccountNum", _loggedInAccountNum);
                    using (SqlDataReader reader = await cmdMetrics.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // This is the bridge: it takes the raw data and saves it to the properties
                            int total = reader["TotalEvents"] != DBNull.Value ? Convert.ToInt32(reader["TotalEvents"]) : 0;
                            int joined = reader["JoinedEvents"] != DBNull.Value ? Convert.ToInt32(reader["JoinedEvents"]) : 0;

                            // This ensures the screen updates immediately
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                TotalEvents = total;
                                JoinedEvents = joined;
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Database Error", $"Could not load home data: {ex.Message}", "OK");
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
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Mark Read Error: {ex.Message}"); }
    }

    // Button Click Tap Scale Animations
    private async Task AnimateButtonAsync(object buttonObj)
    {
        if (buttonObj is VisualElement element)
        {
            await element.ScaleTo(0.92, 50, Easing.Linear);
            await element.ScaleTo(1.0, 50, Easing.Linear);
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
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}