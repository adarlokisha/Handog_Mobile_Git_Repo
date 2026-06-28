using Handog_MobileApp.Models; // Keeps model access uniform
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Handog_MobileApp.ViewModels.Volunteer
{
    public class V_EventsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Event> _displayedEvents;

        public ObservableCollection<Event> DisplayedEvents
        {
            get => _displayedEvents;
            set { _displayedEvents = value; OnPropertyChanged(); }
        }

        public ICommand FilterCommand { get; }
        public ICommand RegisterCommand { get; }

        public V_EventsViewModel()
        {
            DisplayedEvents = new ObservableCollection<Event>();
            FilterCommand = new Command<string>(ExecuteFilterCommand);
            RegisterCommand = new Command<Event>(ExecuteRegisterCommand);

            LoadEventsFromDatabase();
        }

        private void LoadEventsFromDatabase()
        {
            DisplayedEvents.Clear();

            // Mock entry setup corresponding to image_23f305.png design constraints
            DisplayedEvents.Add(new Event
            {
                EventTitle = "LOREM IPSUM DOLOR",
                EventDate = new DateTime(2026, 8, 21),
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(13, 30, 0),
                EventDescription = "Detail: Face Masks and Hairnets Provided!"
            });
        }

        private void ExecuteFilterCommand(string filterType)
        {
            // Database routing query modifications go here
        }

        private void ExecuteRegisterCommand(Event selectedEvent)
        {
            // Database write transactions go here
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}