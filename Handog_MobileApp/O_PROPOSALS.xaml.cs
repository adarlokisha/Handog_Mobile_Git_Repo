using Microsoft.Maui.Controls;
using System;
using static Android.Provider.CalendarContract;
using static Android.Provider.ContactsContract;

namespace Handog_MobileApp
{
    public partial class O_PROPOSALS : ContentPage
    {
        public O_PROPOSALS()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
        }

        private async void BackBtn_Clicked(object sender, EventArgs e)
        {
            await AnimateButton(sender as ImageButton);
            await Navigation.PopAsync();
        }

        private async void HomeBtn_Clicked(object sender, EventArgs e)
        {
            await AnimateButton(sender as ImageButton);
            await Navigation.PushAsync(new O_HOME());
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