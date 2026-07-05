using Handog_MobileApp.ViewModels.Organizer;
using Handog_MobileApp.ViewModels.Volunteer;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp.Views.Volunteer
{
    public partial class V_PROFILE : ContentPage
    {
        private readonly V_ProfileViewModel _viewModel;

        public V_PROFILE(int sessionAccountNum)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            _viewModel = new V_ProfileViewModel(sessionAccountNum, Navigation, this);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadProfileDataAsync();
        }
    }
}