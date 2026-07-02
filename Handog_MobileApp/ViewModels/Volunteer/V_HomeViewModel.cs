using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;


namespace Handog_MobileApp.ViewModels;

public class V_HomeViewModel : INotifyPropertyChanged
{
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

    public ICommand NavigateToProposalsCommand { get; }
    public ICommand NavigateToEventsCommand { get; }

    public V_HomeViewModel(INavigation navigation, int accountNum)
    {
        _navigation = navigation;
        _loggedInAccountNum = accountNum;

        NavigateToProposalsCommand = new Command<object>(async (btn) => await ExecuteNavigateToProposals(btn));
        NavigateToEventsCommand = new Command<object>(async (btn) => await ExecuteNavigateToEvents(btn));
    }

    public async Task InitializeAsync()
    {
        if (_loggedInAccountNum <= 0) return;

        try
        {
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                await conn.OpenAsync();

                string query = "SELECT Firstname FROM ACCOUNT WHERE AccountNum = @AccountNum";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AccountNum", _loggedInAccountNum);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result != null)
                    {
                        WelcomeName = result.ToString();

                        // Dynamically update dashboard metrics fields
                        int joined = 29;
                        int total = 34;
                        double rate = Math.Round((double)joined / total * 100);

                        ParticipationText = $"You've joined {joined} out of {total} events!";
                        ParticipationPercentage = $"{rate}%";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Database Error", ex.Message, "OK");
        }
    }

    private async Task ExecuteNavigateToProposals(object buttonObj)
    {
        if (buttonObj is ImageButton button)
        {
            await button.ScaleTo(0.92, 50, Easing.Linear);
            await button.ScaleTo(1.0, 50, Easing.Linear);
        }

        await _navigation.PushAsync(new V_PROPOSALS(_loggedInAccountNum));
    }

    private async Task ExecuteNavigateToEvents(object buttonObj)
    {
        if (buttonObj is ImageButton button)
        {
            await button.ScaleTo(0.92, 50, Easing.Linear);
            await button.ScaleTo(1.0, 50, Easing.Linear);
        }

        // Redirects control flow forward while carrying forward matching contextual states
        // Replace V_EVENTS with your exact class name if named differently (e.g., V_HOME_EVENTS)
        await _navigation.PushAsync(new Handog_MobileApp.Views.Volunteer.V_EVENTS(_loggedInAccountNum));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}