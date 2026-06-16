using System.Collections.ObjectModel;

namespace Handog_MobileApp;

public partial class V_PROPOSALS : ContentPage
{
    public ObservableCollection<ProposalModel> Proposals { get; set; }

    public V_PROPOSALS()
    {
        InitializeComponent();

        Proposals = new ObservableCollection<ProposalModel>
            {
                new ProposalModel { RequestorName = "NAME OF REQUESTOR", RequestType = "HEALTH RELATED", Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore." },
                new ProposalModel { RequestorName = "NAME OF REQUESTOR", RequestType = "FEEDING PROGRAM", Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore." },
                new ProposalModel { RequestorName = "NAME OF REQUESTOR", RequestType = "LITERACY", Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore." }
            };

        ProposalsCollectionView.ItemsSource = Proposals;
    }

    // Top Back Arrow Behavior
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        if (FormViewContainer.IsVisible)
        {
            // If they are on the Form, return back to the List View
            FormViewContainer.IsVisible = true;
            ListViewContainer.IsVisible = true;
        }
        else
        {
            // If they are already on the List, exit back to previous application page
            await Navigation.PopAsync();
        }
    }

    private async void HomeBtn_Clicked(object sender, EventArgs e)
    {
        await AnimateButton(sender as ImageButton);
        await Navigation.PushAsync(new V_HOME());
    }

    // Toggles display to show the Add Proposal Form layout
    private void OnAddProposalClicked(object sender, EventArgs e)
    {
        ListViewContainer.IsVisible = false;
        FormViewContainer.IsVisible = true;
    }

    private async void OnSaveDraftClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Draft Saved", "Your proposal workspace draft state has been updated.", "OK");

        // Swap back view state to list area
        FormViewContainer.IsVisible = false;
        ListViewContainer.IsVisible = true;
    }

    private async void OnSubmitProposalClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EventTitleEntry.Text) || EventTypePicker.SelectedItem == null)
        {
            await DisplayAlert("Incomplete Form", "Please fill out the Event Type and Title field parameters.", "OK");
            return;
        }

        // Append the new proposal to your list dynamically
        Proposals.Add(new ProposalModel
        {
            RequestorName = "YOU (VOLUNTEER)",
            RequestType = EventTypePicker.SelectedItem.ToString().ToUpper(),
            Description = EventDescriptionEditor.Text ?? "No description provided."
        });

        await DisplayAlert("Success", "Proposal submitted to organizers!", "OK");

        // Clean up inputs
        EventTitleEntry.Text = string.Empty;
        EventDescriptionEditor.Text = string.Empty;
        BeneficiariesEntry.Text = string.Empty;
        LocationEntry.Text = string.Empty;
        EventTypePicker.SelectedItem = null;

        // Go back to the updated list view container
        FormViewContainer.IsVisible = false;
        ListViewContainer.IsVisible = true;
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


public class ProposalModel
{
    public string RequestorName { get; set; }
    public string RequestType { get; set; }
    public string Description { get; set; }
}
