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

        private async void OnProfileImageTapped(object sender, EventArgs e)
        {
            if (BindingContext is V_ProfileViewModel vm)
            {
                string action = await DisplayActionSheet("Profile Photo", "Cancel", "Remove Current Photo", "Upload New Photo");

                if (action == "Upload New Photo")
                {
                    if (vm.UploadProfilePictureCommand.CanExecute(null))
                        vm.UploadProfilePictureCommand.Execute(null);
                }
                else if (action == "Remove Current Photo")
                {
                    if (vm.DeleteProfilePictureCommand.CanExecute(null))
                        vm.DeleteProfilePictureCommand.Execute(null);
                }
            }
        }
    }
}