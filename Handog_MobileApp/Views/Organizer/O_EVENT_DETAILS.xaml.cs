using System;
using Microsoft.Maui.Controls;
using Handog_MobileApp.Models;

namespace Handog_MobileApp.Views.Organizer
{
    public partial class O_EVENT_DETAILS : ContentPage
    {
        private readonly EventDetailsViewModel _viewModel;

        public O_EVENT_DETAILS(EventModel selectedEvent)
        {
            InitializeComponent();

            // Connect the XAML to the ViewModel
            _viewModel = new EventDetailsViewModel(selectedEvent);
            BindingContext = _viewModel;
        }

        // This method automatically runs right as the page appears on the screen
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // This grabs the attendees from the database and magically populates your CollectionView table!
            await _viewModel.LoadAttendeesAsync();
        }
    }
}