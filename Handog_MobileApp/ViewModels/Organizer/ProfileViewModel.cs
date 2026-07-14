using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Handog_MobileApp.Views.Organizer;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace Handog_MobileApp.ViewModels.Organizer
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly int _currentOrganizerAccountNum;
        private readonly INavigation _navigation;
        private readonly Microsoft.Maui.Controls.Page _page;

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

        [ObservableProperty]
        private string qrCodeImageUrl = "qr_placeholder_wireframe.png";

        // --- NEW PROFILE IMAGE PROPERTIES ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasProfileImage))]
        [NotifyPropertyChangedFor(nameof(HasNoProfileImage))]
        private string profileImageUrl;

        // These booleans control which UI element shows in the XAML Grid
        public bool HasProfileImage => !string.IsNullOrEmpty(ProfileImageUrl);
        public bool HasNoProfileImage => string.IsNullOrEmpty(ProfileImageUrl);

        public ProfileViewModel(int accountNum, INavigation navigation, Microsoft.Maui.Controls.Page page)
        {
            _currentOrganizerAccountNum = accountNum;
            _navigation = navigation;
            _page = page;
        }

        public async Task LoadProfileDataAsync()
        {
            if (_currentOrganizerAccountNum == 0) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. Fetch personal details AND the profile picture URL
                    string profileSql = "SELECT Account_ID, Firstname, Lastname, ProfilePicUrl FROM ACCOUNT WHERE AccountNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(profileSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", _currentOrganizerAccountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string firstName = reader["Firstname"].ToString();
                                string lastName = reader["Lastname"].ToString();

                                HeaderUsername = $"{firstName}!";
                                FullName = $"{firstName} {lastName}".ToUpper();
                                AccountId = reader["Account_ID"].ToString();
                                ProfileImageUrl = reader["ProfilePicUrl"]?.ToString();
                                QrCodeImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={AccountId}";
                            }
                        }
                    }

                    // 2. Count "Organized" (ONLY COMPLETED EVENTS)
                    string organizedCountSql = "SELECT COUNT(*) FROM EVENT WHERE OrganizerNum = @AccNum AND EventStatus = 'Completed'";
                    using (SqlCommand cmdOrg = new SqlCommand(organizedCountSql, conn))
                    {
                        cmdOrg.Parameters.AddWithValue("@AccNum", _currentOrganizerAccountNum);
                        CountOrganized = (int)await cmdOrg.ExecuteScalarAsync();
                    }

                    // 3. Count "Joined" (Applying the exact same rule: ONLY COMPLETED EVENTS)
                    string joinedCountSql = @"
                                            SELECT COUNT(*) 
                                            FROM EVENTREGISTRATION r
                                            INNER JOIN EVENT e ON r.EventNum = e.EventNum
                                            WHERE r.AccountNum = @AccNum 
                                              AND e.EventStatus = 'Completed'
                                              AND r.AttendanceStatus = 'Present'";

                    using (SqlCommand cmdJoined = new SqlCommand(joinedCountSql, conn))
                    {
                        cmdJoined.Parameters.AddWithValue("@AccNum", _currentOrganizerAccountNum);
                        int volunteerJoinedCount = (int)await cmdJoined.ExecuteScalarAsync();

                        // THE FIX: Total Joined = Events Organized + Events Volunteered
                        CountJoined = CountOrganized + volunteerJoinedCount;
                    }

                    // You can implement absences later by checking r.AttendanceStatus = 'Absent'
                    CountAbsences = 0;
                }
            }
            catch (Exception ex)
            {
                await _page.DisplayAlert("Database Fault", $"Could not load profile statistics: {ex.Message}", "OK");
            }
        }

        // --- NEW CLOUDINARY UPLOAD LOGIC ---
        [RelayCommand]
        private async Task UploadProfilePictureAsync()
        {
            try
            {
                // 1. Pick the photo
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return;

                using var stream = await photo.OpenReadAsync();

                // 2. Prepare C#'s built-in HttpClient
                using var client = new HttpClient();
                using var content = new MultipartFormDataContent();

                // 3. THE FIX: Stop C# from adding hidden headers!
                var presetContent = new StringContent("effpkvoa");
                presetContent.Headers.ContentType = null; // Forcefully strip the header
                content.Add(presetContent, "\"upload_preset\""); // Wrap the name in quotes to be safe

                // 4. Add the file stream
                var fileContent = new StreamContent(stream);
                content.Add(fileContent, "\"file\"", $"\"{photo.FileName}\"");

                // 5. POST directly to your exact Cloudinary URL
                string uploadUrl = "https://api.cloudinary.com/v1_1/ahewabql/image/upload";
                var response = await client.PostAsync(uploadUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    // 6. Quickly parse the JSON response to grab the secure URL
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                    string newImageUrl = jsonDoc.RootElement.GetProperty("secure_url").GetString();

                    // 7. Update your SQL Database
                    using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                    {
                        await conn.OpenAsync();
                        string sql = "UPDATE ACCOUNT SET ProfilePicUrl = @url WHERE AccountNum = @AccNum";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@url", newImageUrl);
                            cmd.Parameters.AddWithValue("@AccNum", _currentOrganizerAccountNum);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // 8. Instantly update the UI!
                    ProfileImageUrl = newImageUrl;
                }
                else
                {
                    // If it fails, read the exact error message from Cloudinary
                    string errorText = await response.Content.ReadAsStringAsync();
                    await _page.DisplayAlert("Upload Error", $"Cloudinary refused the upload: {errorText}", "OK");
                }
            }
            catch (Exception ex)
            {
                await _page.DisplayAlert("Error", $"Could not upload image: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task GoBackAsync() => await _navigation.PopAsync();

        [RelayCommand]
        private async Task GoHomeAsync() => await _navigation.PushAsync(new O_HOME(_currentOrganizerAccountNum));

        [RelayCommand]
        private async Task GoProposalsAsync() => await _navigation.PushAsync(new O_PROPOSALS(_currentOrganizerAccountNum));

        [RelayCommand]
        private async Task GoEventsAsync() => await _navigation.PushAsync(new O_EVENTS(_currentOrganizerAccountNum));

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