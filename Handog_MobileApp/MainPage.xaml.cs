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
            await Navigation.PushAsync(new V_HOME());
        }
    }
}