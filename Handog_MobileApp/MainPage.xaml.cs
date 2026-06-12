using System;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // FIXED: Renamed to match the XAML exactly
        private async void OClick_Clicked(object sender, EventArgs e)
        {
            // Note: Make sure your target page name in the new repo is still exactly O_HOME
            await Navigation.PushAsync(new O_HOME());
        }

        // FIXED: Added the missing handler for the Volunteer navigation route
        private async void VClick_Clicked(object sender, EventArgs e)
        {
            // Replace 'V_HOME' with whatever your actual volunteer home page class is named
            // await Navigation.PushAsync(new V_HOME());
            await DisplayAlert("Volunteer", "Volunteer flow initiated.", "OK");
        }
    }
}