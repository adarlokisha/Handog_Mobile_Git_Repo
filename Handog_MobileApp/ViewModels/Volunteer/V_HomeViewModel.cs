using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;
using Handog_MobileApp.Views.Volunteer;


namespace Handog_MobileApp.ViewModels;

public class V_HomeViewModel : INotifyPropertyChanged
{
    public ICommand NavigateToHomeCommand { get; }
    public ICommand NavigateToEventsCommand { get; }
    public ICommand NavigateToProposalsCommand { get; }
    public ICommand NavigateToProfileCommand { get; }

    private readonly int _loggedInAccountNum;
    private readonly INavigation _navigation;

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

    
    public V_HomeViewModel(INavigation navigation, int accountNum)
    {
        _navigation = navigation;
        _loggedInAccountNum = accountNum;

        NavigateToHomeCommand = new Command<object>(async (btn) => await ExecuteNavigateToHome(btn));
        NavigateToEventsCommand = new Command<object>(async (btn) => await ExecuteNavigateToEvents(btn));
        NavigateToProposalsCommand = new Command<object>(async (btn) => await ExecuteNavigateToProposals(btn));
        NavigateToProfileCommand = new Command<object>(async (btn) => await ExecuteNavigateToProfile(btn));
    }

    public async Task InitializeAsync()
    {
        if (_loggedInAccountNum <= 0) return;

        try
        {
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                await conn.OpenAsync();

                // 1. Fetch user's first name
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

                // 2. Query actual registration stats using the schema criteria
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

                            // 3. Calculate participation rate safely (prevent division by zero)
                            double rate = total > 0 ? Math.Round((double)joined / total * 100) : 0;

                            // Update databound property values
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
        await _navigation.PushAsync(new V_EVENTS(_loggedInAccountNum)); // Assuming V_EVENTS matches your page name
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