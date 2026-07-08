namespace Handog_MobileApp;
public partial class SignUpPage : ContentPage
{
    private string selectedRole = string.Empty;

    public SignUpPage()
    {
        InitializeComponent();
    }

    // Volunteer tapped
    private void OnVolunteerTapped(object sender, EventArgs e)
    {
        selectedRole = "Volunteer";
        HighlightSelection("Volunteer");
    }

    // Organizer tapped
    private void OnOrganizerTapped(object sender, EventArgs e)
    {
        selectedRole = "Organizer";
        HighlightSelection("Organizer");
    }

    // Admin tapped
    private void OnAdminTapped(object sender, EventArgs e)
    {
        selectedRole = "Admin";
        HighlightSelection("Admin");
    }

    // Continue button
    private async void OnContinueClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(selectedRole))
        {
            await DisplayAlert("Error", "Please select a role before continuing.", "OK");
            return;
        }

        // Show confirmation
        await DisplayAlert("Selected", $"You chose: {selectedRole}", "OK");

        // Example navigation after signup
        // Replace HomePage with your actual next page
        //await Navigation.PushAsync(new HomePage());
    }

    // Helper to highlight selected option
    private void HighlightSelection(string role)
    {
        // Reset all frames to default
        VolunteerFrame.BackgroundColor = Colors.Transparent;
        OrganizerFrame.BackgroundColor = Colors.Transparent;
        //AdminFrame.BackgroundColor = Colors.Transparent;

        // Highlight chosen frame
        switch (role)
        {
            case "Volunteer":
                VolunteerFrame.BackgroundColor = Colors.LightBlue;
                break;
            case "Organizer":
                OrganizerFrame.BackgroundColor = Colors.LightGreen;
                break;
            case "Admin":
                //AdminFrame.BackgroundColor = Colors.LightYellow;
                break;
        }
    }
}