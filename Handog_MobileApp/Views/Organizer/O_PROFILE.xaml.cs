using Microsoft.Maui.Controls;
using Handog_MobileApp.ViewModels.Organizer;

namespace Handog_MobileApp.Views.Organizer
{
    public partial class O_PROFILE : ContentPage
    {
        private readonly ProfileViewModel _viewModel;

        public O_PROFILE(int sessionAccountNum)
        {
            InitializeComponent();

            // Instantiate our ViewModel, passing the necessary context
            _viewModel = new ProfileViewModel(sessionAccountNum, Navigation, this);

            // Set the BindingContext so the XAML knows where to find its data
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Tell the ViewModel to start fetching the data and the QR Code URL
            await _viewModel.LoadProfileDataAsync();
        }
    }
}