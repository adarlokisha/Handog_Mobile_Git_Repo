using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Handog_MobileApp.Views.Organizer;

namespace Handog_MobileApp.ViewModels.Organizer
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly int _currentOrganizerAccountNum;
        private readonly INavigation _navigation;
        private readonly Page _page;

        // Observable properties - XAML will bind to these automatically
        [ObservableProperty]
        private string headerUsername = "Organizer!";

        [ObservableProperty]
        private string fullName = "Loading Name...";

        [ObservableProperty]
        private string accountId = "Loading ID...";

        [ObservableProperty]
        private int countOrganized = 0;

        [ObservableProperty]
        private int countJoined = 0;

        [ObservableProperty]
        private int countAbsences = 0;

        // This property will hold our REST API link for GoQR
        [ObservableProperty]
        private string qrCodeImageUrl = "qr_placeholder_wireframe.png"; // Default placeholder

        public ProfileViewModel(int accountNum, INavigation navigation, Page page)
        {
            _currentOrganizerAccountNum = accountNum;
            _navigation = navigation;
            _page = page;
        }

        // Method to fetch data, called when the page appears
        public async Task LoadProfileDataAsync()
        {
            if (_currentOrganizerAccountNum == 0)
            {
                await _page.DisplayAlert("Error", "Account ID missing. The profile cannot load.", "OK");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. Fetch personal details
                    string profileSql = "SELECT Account_ID, Firstname, Lastname FROM ACCOUNT WHERE AccountNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(profileSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", _currentOrganizerAccountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string firstName = reader["Firstname"].ToString();
                                string lastName = reader["Lastname"].ToString();

                                // Automatically updates XAML UI via Data Binding
                                HeaderUsername = $"{firstName}!";
                                FullName = $"{firstName} {lastName}".ToUpper();
                                AccountId = reader["Account_ID"].ToString();

                                // --- REST API INTEGRATION ---
                                // Dynamically generate the GoQR Image URL using the fetched AccountId
                                QrCodeImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={AccountId}";
                            }
                        }
                    }

                    // 2. Count "Organized"
                    string organizedCountSql = "SELECT COUNT(*) FROM EVENT WHERE OrganizerNum = @AccNum";
                    using (SqlCommand cmdOrg = new SqlCommand(organizedCountSql, conn))
                    {
                        cmdOrg.Parameters.AddWithValue("@AccNum", _currentOrganizerAccountNum);
                        CountOrganized = (int)await cmdOrg.ExecuteScalarAsync();
                    }

                    // 3. Count "Joined"
                    string joinedCountSql = "SELECT COUNT(*) FROM EVENT WHERE OrganizerNum = @AccNum AND EventStatus = 'Completed'";
                    using (SqlCommand cmdJoined = new SqlCommand(joinedCountSql, conn))
                    {
                        cmdJoined.Parameters.AddWithValue("@AccNum", _currentOrganizerAccountNum);
                        CountJoined = (int)await cmdJoined.ExecuteScalarAsync();
                    }

                    // 4. Count "Absences"
                    CountAbsences = 0;
                }
            }
            catch (Exception ex)
            {
                await _page.DisplayAlert("Database Fault", $"Could not load profile statistics: {ex.Message}", "OK");
            }
        }

        // RelayCommands automatically wire up to Button Commands in XAML
        [RelayCommand]
        private async Task GoBackAsync()
        {
            await _navigation.PopAsync();
        }

        [RelayCommand]
        private async Task GoHomeAsync()
        {
            await _navigation.PushAsync(new O_HOME(_currentOrganizerAccountNum));
        }

        [RelayCommand]
        private async Task GoProposalsAsync()
        {
            await _navigation.PushAsync(new O_PROPOSALS(_currentOrganizerAccountNum));
        }

        [RelayCommand]
        private async Task GoEventsAsync()
        {
            await _navigation.PushAsync(new O_EVENTS(_currentOrganizerAccountNum));
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            bool confirm = await _page.DisplayAlert("Logout Confirmation", "Are you sure you want to exit your session?", "Logout", "Cancel");
            if (confirm)
            {
                await _navigation.PopToRootAsync();
            }
        }
    }
}
