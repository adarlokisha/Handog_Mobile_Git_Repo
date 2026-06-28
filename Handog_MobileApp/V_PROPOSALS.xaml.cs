using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;

namespace Handog_MobileApp;

public partial class V_PROPOSALS : ContentPage
{
    private int loggedInAccountNum;
    public ObservableCollection<ProposalModel> Proposals { get; set; }

    public V_PROPOSALS(int accountNum)
    {
        InitializeComponent();
        this.loggedInAccountNum = accountNum;

        Proposals = new ObservableCollection<ProposalModel>();
        ProposalsCollectionView.ItemsSource = Proposals;

        // Load data on page entry
        _ = LoadProposalsFromDatabase();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadProposalsFromDatabase();
    }

    private async Task LoadProposalsFromDatabase()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                await conn.OpenAsync();
                // Ensure these column names exist in your EVENTPROPOSAL table
                string query = "SELECT ProposalTitle, ProposalDetails FROM EVENTPROPOSAL WHERE AccountNum = @AccountNum";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AccountNum", loggedInAccountNum);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        Proposals.Clear();
                        while (await reader.ReadAsync())
                        {
                            Proposals.Add(new ProposalModel
                            {
                                RequestorName = reader["RequestorName"]?.ToString() ?? "Unknown",
                                RequestType = reader["RequestType"]?.ToString() ?? "General",
                                Description = reader["Description"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Database Error", $"Could not load proposals: {ex.Message}", "OK");
        }
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        if (FormViewContainer.IsVisible)
        {
            FormViewContainer.IsVisible = false;
            ListViewContainer.IsVisible = true;
        }
        else
        {
            await Navigation.PopAsync();
        }
    }

    private async void HomeBtn_Clicked(object sender, EventArgs e)
    {
        await AnimateButton(sender as ImageButton);
        await Navigation.PushAsync(new V_HOME(loggedInAccountNum));
    }

    private void OnAddProposalClicked(object sender, EventArgs e)
    {
        ListViewContainer.IsVisible = false;
        FormViewContainer.IsVisible = true;
    }

    private async void OnSaveDraftClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Draft Saved", "Your proposal workspace draft state has been updated.", "OK");
        FormViewContainer.IsVisible = false;
        ListViewContainer.IsVisible = true;
    }

    private int GetCategoryNum(string categoryName) => categoryName switch
    {
        "Medical Mission" => 1,
        "Feeding Program" => 2,
        "Youth Activity" => 3,
        "Spiritual Gathering" => 4,
        "Environmental Care" => 5,
        _ => 1
    };

    private async void OnSubmitProposalClicked(object sender, EventArgs e)
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(EventTitleEntry.Text) || EventTypePicker.SelectedItem == null)
        {
            await DisplayAlert("Incomplete Form", "Please fill out the Event Type and Title field.", "OK");
            return;
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                await conn.OpenAsync();

                // 2. Generate Proposal_ID (e.g., PR001)
                string countSql = "SELECT ISNULL(MAX(ProposalNum), 0) + 1 FROM EVENTPROPOSAL";
                int nextId = (int)await new SqlCommand(countSql, conn).ExecuteScalarAsync();
                string proposalIdFormatted = "PR" + nextId.ToString("D3");

                // 3. Insert into database
                // Note: I mapped 'CategoryNum' to a placeholder. 
                // If your logic requires specific IDs for categories, use a switch statement on the selected string.
                string insertQuery = @"INSERT INTO EVENTPROPOSAL 
                                   (Proposal_ID, AccountNum, CategoryNum, ProposalTitle, ProposalDetails, 
                                    ProposalStatus, DateCreated) 
                                   VALUES (@ID, @Account, @Cat, @Title, @Details, 'Pending', GETDATE())";

                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", proposalIdFormatted);
                    cmd.Parameters.AddWithValue("@Account", loggedInAccountNum);
                    cmd.Parameters.AddWithValue("@Cat", 1); // Replace '1' with mapping logic based on EventTypePicker selection
                    cmd.Parameters.AddWithValue("@Title", EventTitleEntry.Text);
                    cmd.Parameters.AddWithValue("@Details",
                        $"Description: {EventDescriptionEditor.Text ?? ""}\n" +
                        $"Beneficiaries: {BeneficiariesEntry.Text ?? "0"}\n" +
                        $"Location: {LocationEntry.Text ?? "Not specified"}");

                    await cmd.ExecuteNonQueryAsync();
                }

                await DisplayAlert("Success", $"Proposal {proposalIdFormatted} submitted!", "OK");

                // 4. Clear UI
                EventTitleEntry.Text = string.Empty;
                EventDescriptionEditor.Text = string.Empty;
                BeneficiariesEntry.Text = string.Empty;
                LocationEntry.Text = string.Empty;
                EventTypePicker.SelectedItem = null;

                await LoadProposalsFromDatabase();
                FormViewContainer.IsVisible = false;
                ListViewContainer.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not submit proposal: " + ex.Message, "OK");
        }
    }

    private async Task AnimateButton(ImageButton button)
    {
        if (button != null)
        {
            await button.ScaleTo(0.92, 50, Easing.Linear);
            await button.ScaleTo(1.0, 50, Easing.Linear);
        }
    }

    private void SetTabActive(Border activeBorder)
    {
        TabMyProposals.BackgroundColor = Colors.Transparent;
        TabAllProposals.BackgroundColor = Colors.Transparent;
        TabApprovedProposals.BackgroundColor = Colors.Transparent;
        activeBorder.BackgroundColor = Colors.White;
    }

    private void MyProposalsTab_Tapped(object sender, EventArgs e) => SetTabActive(TabMyProposals);
    private void AllProposalsTab_Tapped(object sender, EventArgs e) => SetTabActive(TabAllProposals);
    private void ApprovedProposalsTab_Tapped(object sender, EventArgs e) => SetTabActive(TabApprovedProposals);
}

public class ProposalModel
{
    public string RequestorName { get; set; }
    public string RequestType { get; set; }
    public string Description { get; set; }
}