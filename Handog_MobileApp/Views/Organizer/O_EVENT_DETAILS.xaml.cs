using Microsoft.Maui.Controls;
using Handog_MobileApp.Models;

namespace Handog_MobileApp.Views.Organizer
{
    public partial class O_EVENT_DETAILS : ContentPage
    {
        private readonly EventDetailsViewModel _viewModel;

        // This correctly catches the EventModel sent from the main events page
        public O_EVENT_DETAILS(EventModel selectedEvent)
        {
            InitializeComponent();

            // Link the specific event data into the ViewModel engine
            _viewModel = new EventDetailsViewModel(selectedEvent);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Fetch the volunteers the moment this page slides onto the screen
            await _viewModel.LoadAttendeesAsync();
        }
    }
}