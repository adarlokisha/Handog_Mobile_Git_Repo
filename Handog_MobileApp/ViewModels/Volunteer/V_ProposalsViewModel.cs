using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Handog_MobileApp.Models;

namespace Handog_MobileApp.ViewModels.Volunteer
{
    public class V_ProposalsViewModel : INotifyPropertyChanged
    {
        private readonly int _loggedInAccountNum;
        private readonly INavigation _navigation;

        private bool _isListViewVisible = true;
        private bool _isFormViewVisible = false;

        // Modified form binding fields matching schema requirements
        private string _selectedCategory = string.Empty;
        private string _proposalTitle = string.Empty;
        private DateTime _preferredDate = DateTime.Today;
        private TimeSpan _preferredStartTime = TimeSpan.Zero;
        private TimeSpan _preferredEndTime = TimeSpan.Zero;
        private string _proposalDetails = string.Empty;

        public ObservableCollection<ProposalModel> Proposals { get; } = new();

        public bool IsListViewVisible
        {
            get => _isListViewVisible;
            set { _isListViewVisible = value; OnPropertyChanged(); }
        }

        public bool IsFormViewVisible
        {
            get => _isFormViewVisible;
            set { _isFormViewVisible = value; OnPropertyChanged(); }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public string ProposalTitle
        {
            get => _proposalTitle;
            set { _proposalTitle = value; OnPropertyChanged(); }
        }

        public DateTime PreferredDate
        {
            get => _preferredDate;
            set { _preferredDate = value; OnPropertyChanged(); }
        }

        public TimeSpan PreferredStartTime
        {
            get => _preferredStartTime;
            set { _preferredStartTime = value; OnPropertyChanged(); }
        }

        public TimeSpan PreferredEndTime
        {
            get => _preferredEndTime;
            set { _preferredEndTime = value; OnPropertyChanged(); }
        }

        public string ProposalDetails
        {
            get => _proposalDetails;
            set { _proposalDetails = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand LoadProposalsCommand { get; }
        public ICommand ShowAddFormCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand SubmitProposalCommand { get; }
        public ICommand CancelFormCommand { get; }

        // Navigation Commands
        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToEventsCommand { get; }
        public ICommand NavigateToProfileCommand { get; }

        public event Action<string, string> ShowAlertRequested;

        public V_ProposalsViewModel(INavigation navigation, int accountNum)
        {
            _navigation = navigation;
            _loggedInAccountNum = accountNum;

            LoadProposalsCommand = new Command(async () => await LoadProposalsFromDatabaseAsync());
            ShowAddFormCommand = new Command(() => { IsListViewVisible = false; IsFormViewVisible = true; });
            CancelFormCommand = new Command(() => { IsFormViewVisible = false; IsListViewVisible = true; });
            SaveDraftCommand = new Command(ExecuteSaveDraft);
            SubmitProposalCommand = new Command(async () => await ExecuteSubmitProposalAsync());

            NavigateToHomeCommand = new Command<object>(async (btn) => await ExecuteNavigateToHome(btn));
            NavigateToEventsCommand = new Command<object>(async (btn) => await ExecuteNavigateToEvents(btn));
            NavigateToProfileCommand = new Command<object>(async (btn) => await ExecuteNavigateToProfile(btn));
        }

        private async Task AnimateButtonAsync(object buttonObj)
        {
            if (buttonObj is ImageButton button)
            {
                await button.ScaleTo(0.92, 50, Easing.Linear);
                await button.ScaleTo(1.0, 50, Easing.Linear);
            }
        }

        private async Task ExecuteNavigateToHome(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_HOME(_loggedInAccountNum));
        }

        private async Task ExecuteNavigateToEvents(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new Handog_MobileApp.Views.Volunteer.V_EVENTS(_loggedInAccountNum));
        }

        private async Task ExecuteNavigateToProfile(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
        }

        // Add this property near your other private backing fields
        private string _currentTab = "MyProposals";

        // Add this public property to track the active view context
        public string CurrentTab
        {
            get => _currentTab;
            set { _currentTab = value; OnPropertyChanged(); }
        }

        public async Task LoadProposalsFromDatabaseAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    // Base structural query
                    string query = "SELECT ProposalTitle AS RequestType, ProposalDetails AS Description, AccountNum, ProposalStatus FROM EVENTPROPOSAL WHERE ";

                    // Apply conditional filters depending on the selected UI tab status context
                    if (CurrentTab == "MyProposals")
                    {
                        // Shows ALL proposals belonging to the logged-in user regardless of status
                        query += "AccountNum = @AccountNum";
                    }
                    else if (CurrentTab == "AllProposals")
                    {
                        // Shows items belonging to the logged-in user OR items from anyone that are already Approved
                        query += "(AccountNum = @AccountNum OR ProposalStatus = 'Approved')";
                    }
                    else if (CurrentTab == "ApprovedProposals")
                    {
                        // Strictly displays only approved proposals globally
                        query += "ProposalStatus = 'Approved'";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Add parameter safe-guards
                        cmd.Parameters.AddWithValue("@AccountNum", _loggedInAccountNum);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            Proposals.Clear();
                            while (await reader.ReadAsync())
                            {
                                // Identify if row belongs to the current user or someone else
                                int itemOwnerId = Convert.ToInt32(reader["AccountNum"]);
                                string labelName = (itemOwnerId == _loggedInAccountNum) ? "My Proposal" : "Volunteer Proposal";

                                Proposals.Add(new ProposalModel
                                {
                                    RequestorName = labelName,
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
                ShowAlertRequested?.Invoke("Database Error", $"Could not load filtered proposals: {ex.Message}");
            }
        }

        private void ExecuteSaveDraft()
        {
            ShowAlertRequested?.Invoke("Draft Saved", "Your proposal workspace draft state has been updated.");
            IsFormViewVisible = false;
            IsListViewVisible = true;
        }

        private async Task ExecuteSubmitProposalAsync()
        {
            if (string.IsNullOrWhiteSpace(ProposalTitle) || string.IsNullOrWhiteSpace(SelectedCategory))
            {
                ShowAlertRequested?.Invoke("Incomplete Form", "Please select a Category and complete the Event Title field.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    string countSql = "SELECT ISNULL(MAX(ProposalNum), 0) + 1 FROM EVENTPROPOSAL";
                    int nextId = (int)await new SqlCommand(countSql, conn).ExecuteScalarAsync();
                    string proposalIdFormatted = "PR" + nextId.ToString("D3");

                    string insertQuery = @"INSERT INTO EVENTPROPOSAL 
                                       (Proposal_ID, AccountNum, CategoryNum, ProposalTitle, ProposalDetails, 
                                        PreferredDate, PreferredStartTime, PreferredEndTime, ProposalStatus, DateCreated) 
                                       VALUES (@ID, @Account, @Cat, @Title, @Details, @PrefDate, @StartTime, @EndTime, 'Pending', GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", proposalIdFormatted);
                        cmd.Parameters.AddWithValue("@Account", _loggedInAccountNum);
                        cmd.Parameters.AddWithValue("@Cat", GetCategoryNum(SelectedCategory));
                        cmd.Parameters.AddWithValue("@Title", ProposalTitle);
                        cmd.Parameters.AddWithValue("@Details", ProposalDetails ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PrefDate", PreferredDate.Date);
                        cmd.Parameters.AddWithValue("@StartTime", PreferredStartTime);
                        cmd.Parameters.AddWithValue("@EndTime", PreferredEndTime);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    ShowAlertRequested?.Invoke("Success", $"Proposal {proposalIdFormatted} submitted successfully!");

                    // Reset form properties
                    ProposalTitle = string.Empty;
                    ProposalDetails = string.Empty;
                    SelectedCategory = null;
                    PreferredDate = DateTime.Today;
                    PreferredStartTime = TimeSpan.Zero;
                    PreferredEndTime = TimeSpan.Zero;

                    await LoadProposalsFromDatabaseAsync();
                    IsFormViewVisible = false;
                    IsListViewVisible = true;
                }
            }
            catch (Exception ex)
            {
                ShowAlertRequested?.Invoke("Error", "Could not submit proposal: " + ex.Message);
            }
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}