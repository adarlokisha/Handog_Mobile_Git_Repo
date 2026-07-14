using System;
using Handog_MobileApp.ViewModels.Volunteer;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp.Views.Volunteer
{
    public partial class V_EVENTS : ContentPage
    {
        private readonly V_EventsViewModel _viewModel;
        private readonly int _sessionAccountNum;

        // Define your custom accent color globally inside the class
        private readonly Color _accentColor = Color.FromHex("#4DD0E1");

        public V_EVENTS(int sessionAccountNum)
        {
            InitializeComponent();

            _sessionAccountNum = sessionAccountNum;
            _viewModel = new V_EventsViewModel(Navigation, sessionAccountNum);
            BindingContext = _viewModel;

            _viewModel.ShowAlertRequested += OnShowAlertRequested;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.SetUserSession(_sessionAccountNum);
        }

        private async void OnShowAlertRequested(string title, string message)
        {
            await DisplayAlert(title, message, "OK");
        }

        private void MyEventsTab_Tapped(object sender, EventArgs e)
        {
            // Explicitly set the background color to your custom hex color
            TabMyEvents.BackgroundColor = _accentColor;
            TabAllEvents.BackgroundColor = Colors.Transparent;
            TabCompleted.BackgroundColor = Colors.Transparent;


            _viewModel.FilterCommand.Execute("MyEvents");
        }

        private void AllEventsTab_Tapped(object sender, EventArgs e)
        {
            // Explicitly set the background color to your custom hex color
            TabAllEvents.BackgroundColor = _accentColor;
            TabMyEvents.BackgroundColor = Colors.Transparent;
            TabCompleted.BackgroundColor = Colors.Transparent;


            _viewModel.FilterCommand.Execute("AllEvents");
        }

        private void CompletedTab_Tapped(object sender, EventArgs e)
        {
            TabCompleted.BackgroundColor = _accentColor;
            TabMyEvents.BackgroundColor = Colors.Transparent;
            TabAllEvents.BackgroundColor = Colors.Transparent;

            _viewModel.FilterCommand.Execute("Completed");
        }
    }
}