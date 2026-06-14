namespace Handog_MobileApp;

public partial class V_HOME : ContentPage
{
	public V_HOME()
	{
		InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
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
