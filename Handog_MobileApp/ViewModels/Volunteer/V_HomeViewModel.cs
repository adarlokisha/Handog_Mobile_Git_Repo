using Handog_MobileApp.Services; // Ensure your NotificationService is accessible here
using Handog_MobileApp.Views.Volunteer;
using Microsoft.Maui.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace Handog_MobileApp.ViewModels;

public class V_HomeViewModel : NotificationViewModel
{
    // Services
    // Note: If NotificationViewModel already instantiates a service named _notificationService,
    // you can safely use it here by changing its access modifier to protected in the base class.

    // Page navigation commands specific to the Volunteer Hub
    public ICommand NavigateToHomeCommand { get; }
    public ICommand NavigateToEventsCommand { get; }
    public ICommand NavigateToProposalsCommand { get; }
    public ICommand NavigateToProfileCommand { get; }

    private readonly int _loggedInAccountNum;
    private readonly INavigation _navigation;

    // View dashboard properties
    private string _welcomeName = "Volunteer";
    public string WelcomeName
    {
        get => _welcomeName;
        set { _welcomeName = value; OnPropertyChanged(); }
    }

    private string _participationText = "Loading details...";
    public string ParticipationText
    {
        get => _participationText;
        set { _participationText = value; OnPropertyChanged(); }
    }

    private string _participationPercentage = "0%";
    public string ParticipationPercentage
    {
        get => _participationPercentage;
        set { _participationPercentage = value; OnPropertyChanged(); }
    }

    public V_HomeViewModel(INavigation navigation, int accountNum) : base()
    {
        _navigation = navigation;
        _loggedInAccountNum = accountNum;

        // Dashboard specific button mappings
        NavigateToHomeCommand = new Command<object>(async (btn) => await ExecuteNavigateToHome(btn));
        NavigateToEventsCommand = new Command<object>(async (btn) => await ExecuteNavigateToEvents(btn));
        NavigateToProposalsCommand = new Command<object>(async (btn) => await ExecuteNavigateToProposals(btn));
        NavigateToProfileCommand = new Command<object>(async (btn) => await ExecuteNavigateToProfile(btn));
    }

    public async Task InitializeAsync()
    {
        if (_loggedInAccountNum <= 0) return;

        // 1. Core shared base notification fetch logic handled by the parent viewmodel
        await RefreshNotificationsAsync(_loggedInAccountNum);

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
                            int total = reader["TotalEvents"] != DBNull.Value ? Convert.ToInt32(reader["TotalEvents"]) : 0;
                            int joined = reader["JoinedEvents"] != DBNull.Value ? Convert.ToInt32(reader["JoinedEvents"]) : 0;

                            // Calculate safe percentage value bounds (prevent zero division crashes)
                            double rate = total > 0 ? Math.Round((double)joined / total * 100) : 0;

                            // Update local dashboard UI property binds
                            ParticipationText = $"You've joined {joined} out of {total} events!";
                            ParticipationPercentage = $"{rate}%";
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
}