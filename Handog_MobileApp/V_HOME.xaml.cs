namespace Handog_MobileApp;

public partial class V_HOME : ContentPage
{
	public V_HOME()
	{
		InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
    }

    private async void ProposalsBtn_Clicked(object sender, EventArgs e)
    {
        await AnimateButton(sender as ImageButton);
        await Navigation.PushAsync(new V_PROPOSALS());
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
