using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Data.SqlClient;
using Handog_MobileApp.Models;

namespace Handog_MobileApp.ViewModels.Volunteer
{
    public class V_EventsViewModel : INotifyPropertyChanged
    {
        private int _loggedInAccountNum;
        private readonly INavigation _navigation;

        public event Action<string, string> ShowAlertRequested;

        public ObservableCollection<EventModel> DisplayedEvents { get; } = new();

        // Commands declared in your XAML
        public ICommand FilterCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToProposalsCommand { get; }

        // Parameterless constructor so your XAML clr-namespace instantiation works flawlessly

        // MATCH THIS CONSTRUCTOR TO YOUR CODE-BEHIND
        public V_EventsViewModel(INavigation navigation, int accountNum)
        {
            _navigation = navigation;
            _loggedInAccountNum = accountNum;

            FilterCommand = new Command<string>(ExecuteFilter);
            RegisterCommand = new Command<EventModel>(async (ev) => await ExecuteRegisterAsync(ev));

            NavigateToHomeCommand = new Command<object>(async (btn) => await ExecuteNavigateToHome(btn));
            NavigateToProposalsCommand = new Command<object>(async (btn) => await ExecuteNavigateToProposals(btn));
        }

        public void SetUserSession(int accountNum)
        {
            _loggedInAccountNum = accountNum;
            _ = LoadEventsFromDatabaseAsync();
        }

        private void ExecuteFilter(string filterType)
        {
            // Logic for switching tabs between "MyEvents" or "AllEvents"
            if (filterType == "MyEvents")
            {
                // Filter database fields or list elements to show personal updates
            }
            else
            {
                // Show general distribution logs
            }
        }

        private async Task ExecuteRegisterAsync(EventModel selectedEvent)
        {
            if (selectedEvent == null) return;

            await Application.Current.MainPage.DisplayAlert("Registration", $"Successfully registered for: {selectedEvent.EventTitle}", "OK");
        }

        private async Task AnimateButtonAsync(object buttonObj)
        {
            if (buttonObj is ImageButton button)
            {
                await button.ScaleTo(0.92, 50, Easing.Linear);
                await button.ScaleTo(1.0, 50, Easing.Linear);
            }
        }

        private async Task ExecuteNavigateToHome(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_HOME(_loggedInAccountNum));
        }

        private async Task ExecuteNavigateToProposals(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_PROPOSALS(_loggedInAccountNum));
        }

        public async Task LoadEventsFromDatabaseAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT EventTitle, EventDetails, Location FROM EVENT";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            DisplayedEvents.Clear();
                            while (await reader.ReadAsync())
                            {
                                DisplayedEvents.Add(new EventModel
                                {
                                    EventTitle = reader["EventTitle"]?.ToString() ?? "New Event",
                                    EventDescription = reader["EventDetails"]?.ToString() ?? "",
                                    Location = reader["Location"]?.ToString() ?? "TBD"
                                    // Add FormattedDate / FormattedTime mappings if your database explicitly breaks them out
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database Error: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}