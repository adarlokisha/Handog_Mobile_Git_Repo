using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Handog_MobileApp.Views.Organizer;
using Handog_MobileApp.Models;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Graphics;

namespace Handog_MobileApp.ViewModel.Organizer
{
    public partial class ProposalsViewModel : BaseViewModel
    {
        private readonly int _currentAccountNum;

        [ObservableProperty]
        private ObservableCollection<O_EventProposal> _proposals = new();

        [ObservableProperty]
        private string _headerUsername;

        // --- NEW TAB PROPERTIES ---
        private string _activeTab = "Pending";

        [ObservableProperty]
        private Color _pendingTabColor = Colors.White;

        [ObservableProperty]
        private Color _completedTabColor = Colors.Transparent;

        [ObservableProperty]
        private bool _isPendingView = true;

        // NEW PROPERTY to replace the missing InvertedBoolConverter
        [ObservableProperty]
        private bool _isCompletedView = false;

        [ObservableProperty]
        private string _listTitle = "PENDING PROPOSALS";

        public ICommand NavigateCommand { get; }

        public ProposalsViewModel(int accountNum)
        {
            _currentAccountNum = accountNum;
            NavigateCommand = new Command<string>(async (dest) => await ExecuteNavigation(dest));
        }

        [RelayCommand]
        public async Task LoadData()
        {
            await FetchOrganizerName();
            await FetchProposals();
        }

        // --- NEW SWITCH TAB COMMAND ---
        [RelayCommand]
        public async Task SwitchTab(string tab)
        {
            _activeTab = tab;
            IsPendingView = tab == "Pending";
            IsCompletedView = !IsPendingView; // Update the new property automatically

            ListTitle = tab == "Pending" ? "PENDING PROPOSALS" : "REVIEWED PROPOSALS";

            PendingTabColor = tab == "Pending" ? Colors.White : Colors.Transparent;
            CompletedTabColor = tab == "Completed" ? Colors.White : Colors.Transparent;

            await FetchProposals();
        }

        private async Task FetchOrganizerName()
        {
            try
            {
                using var conn = new SqlConnection(AppConfig.DbConnectionString);
                await conn.OpenAsync();
                string sql = "SELECT Firstname FROM ACCOUNT WHERE AccountNum = @AccNum";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AccNum", _currentAccountNum);

                var result = await cmd.ExecuteScalarAsync();
                HeaderUsername = result != null ? $"{result}!" : "Organizer!";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching name: {ex.Message}");
            }
        }

        private async Task FetchProposals()
        {
            try
            {
                var list = new List<O_EventProposal>();
                using var conn = new SqlConnection(AppConfig.DbConnectionString);

                string statusFilter = _activeTab == "Pending" ? "='Pending'" : "IN ('Approved', 'Rejected')";

                string sql = $@"SELECT p.ProposalNum, p.Proposal_ID, p.CategoryNum, p.ProposalTitle, p.ProposalDetails, 
                      p.PreferredDate, p.PreferredStartTime, p.PreferredEndTime, p.ProposalStatus,
                      a.Firstname + ' ' + a.Lastname as ProposerName
               FROM EVENTPROPOSAL p
               JOIN ACCOUNT a ON p.AccountNum = a.AccountNum
               WHERE p.ProposalStatus {statusFilter}";

                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new O_EventProposal
                    {
                        ProposalNum = (int)reader["ProposalNum"],
                        Proposal_ID = reader["Proposal_ID"].ToString(),
                        CategoryNum = (int)reader["CategoryNum"],
                        ProposalTitle = reader["ProposalTitle"].ToString(),
                        ProposalDetails = reader["ProposalDetails"].ToString(),
                        ProposerName = reader["ProposerName"].ToString(),
                        PreferredDate = Convert.ToDateTime(reader["PreferredDate"]),
                        PreferredStartTime = (TimeSpan)reader["PreferredStartTime"],
                        PreferredEndTime = (TimeSpan)reader["PreferredEndTime"],
                        ProposalStatus = reader["ProposalStatus"].ToString()
                    });
                }

                // Safe to set a new collection entirely from a background thread
                Proposals = new ObservableCollection<O_EventProposal>(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task Reject(O_EventProposal p)
        {
            try
            {
                using var conn = new SqlConnection(AppConfig.DbConnectionString);
                await conn.OpenAsync();
                string sql = "UPDATE EVENTPROPOSAL SET ProposalStatus = 'Rejected' WHERE ProposalNum = @ProposalNum";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ProposalNum", p.ProposalNum);
                await cmd.ExecuteNonQueryAsync();

                // Safely update the ObservableCollection on the Main UI Thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Proposals.Remove(p);
                });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Could not reject: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task Accept(O_EventProposal p)
        {
            try
            {
                using var conn = new SqlConnection(AppConfig.DbConnectionString);
                await conn.OpenAsync();
                string sql = "UPDATE EVENTPROPOSAL SET ProposalStatus = 'Approved' WHERE ProposalNum = @ProposalNum";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ProposalNum", p.ProposalNum);
                await cmd.ExecuteNonQueryAsync();

                // Safely update the ObservableCollection on the Main UI Thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Proposals.Remove(p);
                });

                await Application.Current.MainPage.Navigation.PushAsync(new O_EVENTS(_currentAccountNum));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Could not accept: {ex.Message}", "OK");
            }
        }

        private async Task ExecuteNavigation(string dest)
        {
            Page targetPage = dest switch
            {
                "Home" => new O_HOME(_currentAccountNum),
                "Proposals" => new O_PROPOSALS(_currentAccountNum),
                "Events" => new O_EVENTS(_currentAccountNum),
                "Profile" => new O_PROFILE(_currentAccountNum),
                _ => null
            };

            if (targetPage != null)
                await Application.Current.MainPage.Navigation.PushAsync(targetPage);
        }
    }
}