using Microsoft.Maui.Controls;
using Handog_MobileApp.ViewModels.Organizer; // Make sure this matches your namespace

namespace Handog_MobileApp.Views.Organizer
{
    public partial class O_HOME : ContentPage
    {
        private OrganizerHomeViewModel _viewModel;

        public O_HOME(int accountNum)
        {
            InitializeComponent();

            // Connect the View to the ViewModel
            _viewModel = new OrganizerHomeViewModel(accountNum, this.Navigation);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Fire off the background database sync when the page loads
            await _viewModel.LoadDashboardDataAsync();
        }
    }
}