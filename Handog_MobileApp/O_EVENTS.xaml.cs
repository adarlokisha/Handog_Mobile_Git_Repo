using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace Handog_MobileApp
{
    public partial class O_EVENTS : ContentPage
    {
        public O_EVENTS()
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
        private async void ProposalsBtn_Clicked(object sender, EventArgs e)
        {
            await AnimateButton(sender as ImageButton);
            await Navigation.PushAsync(new O_PROPOSALS());
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
                // Quick 50ms compression scale down and return bounce
                await button.ScaleTo(0.92, 50, Easing.Linear);
                await button.ScaleTo(1.0, 50, Easing.Linear);
            }
        }
    }
}