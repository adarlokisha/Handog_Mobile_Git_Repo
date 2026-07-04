using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Handog_MobileApp.Views.Organizer;
using Handog_MobileApp.Models;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input; // Required for ICommand

namespace Handog_MobileApp.ViewModel.Organizer
{
    public partial class ProposalsViewModel : BaseViewModel
    {
        private readonly int _currentAccountNum;

        [ObservableProperty]
        private ObservableCollection<O_EventProposal> _proposals = new();

        [ObservableProperty]
        private string _headerUsername;

        // Command for navigation
        public ICommand NavigateCommand { get; }

        // Combined Constructor
        public ProposalsViewModel(int accountNum)
        {
            _currentAccountNum = accountNum;

            // Initialize the navigation command
            NavigateCommand = new Command<string>(async (dest) => await ExecuteNavigation(dest));
        }

        [RelayCommand]
        public async Task LoadData()
        {
            await FetchOrganizerName();
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
                string sql = @"SELECT p.ProposalNum, p.Proposal_ID, p.CategoryNum, p.ProposalTitle, p.ProposalDetails, 
                                      p.PreferredDate, p.PreferredStartTime, p.PreferredEndTime, 
                                      a.Firstname + ' ' + a.Lastname as ProposerName
                               FROM EVENTPROPOSAL p
                               JOIN ACCOUNT a ON p.AccountNum = a.AccountNum
                               WHERE p.ProposalStatus = 'Pending'";

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
                        PreferredEndTime = (TimeSpan)reader["PreferredEndTime"]
                    });
                }
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

                Proposals.Remove(p);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Could not reject: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task Accept(O_EventProposal p)
        {
            try
            {
                await Application.Current.MainPage.Navigation.PushAsync(new O_EVENTS(_currentAccountNum, p));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Navigation Error", ex.Message, "OK");
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