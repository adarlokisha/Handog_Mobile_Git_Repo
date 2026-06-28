using Microsoft.Maui.Controls;

namespace Handog_MobileApp.Views.Volunteer;

public partial class V_EVENTS : ContentPage
{
    private int loggedInAccountNum;

    // Update the constructor to accept the account number
    public V_EVENTS(int accountNum)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);

        // Save it to use for your upcoming database queries on this page!
        this.loggedInAccountNum = accountNum;
    }
}