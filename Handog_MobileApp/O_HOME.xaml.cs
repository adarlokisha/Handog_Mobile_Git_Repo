using Microsoft.Maui.Controls;
using System;

namespace Handog_MobileApp
{
    public partial class O_HOME : ContentPage
    {
        public O_HOME()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
        }

        private async void ProposalsBtn_Clicked(object sender, EventArgs e)
        {
            await AnimateButton(sender as ImageButton);
            await Navigation.PushAsync(new O_PROPOSALS());
        }

        private async void EventsBtn_Clicked(object sender, EventArgs e)
        {
            await AnimateButton(sender as ImageButton);
            await Navigation.PushAsync(new O_EVENTS());
        }

        private async void ProfileBtn_Clicked(object sender, EventArgs e)
        {
            await AnimateButton(sender as ImageButton);
            await Navigation.PushAsync(new O_PROFILE());
        }

        private async Task AnimateButton(ImageButton button)
        {
            if (button != null)
            {
                await button.ScaleTo(0.92, 50, Easing.Linear);
                await button.ScaleTo(1.0, 50, Easing.Linear);
            }
        }
    }
}