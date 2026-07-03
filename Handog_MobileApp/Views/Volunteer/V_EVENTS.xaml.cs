using System;
using Microsoft.Maui.Controls;
using Handog_MobileApp.ViewModels.Volunteer;

namespace Handog_MobileApp.Views.Volunteer
{
    public partial class V_EVENTS : ContentPage
    {
        private readonly V_EventsViewModel _viewModel;

        public V_EVENTS(int accountNum)
        {
            InitializeComponent();
            _viewModel = new V_EventsViewModel(Navigation, accountNum);
            _viewModel.ShowAlertRequested += OnViewModelShowAlertRequested;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ISSUE 1 FIX: Match initial highlight states to the actual default 'AllEvents' criteria load selection
            SetTabActive(TabAllEvents);
            _viewModel.FilterCommand.Execute("AllEvents");
        }

        private async void OnViewModelShowAlertRequested(string title, string message)
        {
            await DisplayAlert(title, message, "OK");
        }

        private void SetTabActive(Border activeBorder)
        {
            if (TabMyEvents == null || TabAllEvents == null) return;

            TabMyEvents.BackgroundColor = Colors.Transparent;
            TabAllEvents.BackgroundColor = Colors.Transparent;

            activeBorder.BackgroundColor = Color.FromArgb("#4DD0E1");
        }

        private void MyEventsTab_Tapped(object sender, EventArgs e)
        {
            SetTabActive(TabMyEvents);
            _viewModel.FilterCommand.Execute("MyEvents");
        }

        private void AllEventsTab_Tapped(object sender, EventArgs e)
        {
            SetTabActive(TabAllEvents);
            _viewModel.FilterCommand.Execute("AllEvents");
        }
    }
}