using Microsoft.Maui.Controls;
using System;

namespace Handog_MobileApp
{
    public partial class O_PROFILE : ContentPage
    {
        public O_PROFILE()
        {
            InitializeComponent();
        }

        private async void BackBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void HomeBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new O_HOME());
        }

        private async void ProposalsBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new O_PROPOSALS());
        }

        private async void EventsBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new O_EVENTS());
        }

        private async void LogoutBtn_Clicked(object sender, EventArgs e)
        {
            //Application.Current.MainPage = new NavigationPage(new LoginPage());
            await DisplayAlert("Logout", "Are you sure you want to logout?.", "OK");
        }
    }
}