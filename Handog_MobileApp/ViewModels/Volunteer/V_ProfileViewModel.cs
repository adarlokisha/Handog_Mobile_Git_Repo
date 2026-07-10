using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;
using Handog_MobileApp.Views.Volunteer;

namespace Handog_MobileApp.ViewModels.Volunteer
{
    // Re-added partial modifier to avoid conflicts with background-cached compiler files
    public partial class V_ProfileViewModel : INotifyPropertyChanged
    {

        public ICommand GoBackCommand { get; }
        public ICommand GoHomeCommand { get; }
        public ICommand GoHistoryCommand { get; }
        public ICommand GoNotificationsCommand { get; }
        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToProposalsCommand { get; } 
        public ICommand NavigateToEventsCommand { get; }
        public ICommand NavigateToProfileCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand UploadProfilePictureCommand { get; }
        public ICommand DeleteProfilePictureCommand { get; }

        private readonly int _currentVolunteerAccountNum;
        private readonly INavigation _navigation;
        private readonly Page _page;

        private string _headerUsername = "Volunteer!";
        private string _fullName = "Loading Name...";
        private string _accountId = "Loading ID...";
        private int _countCompleted = 0;
        private int _countJoined = 0;
        private int _countAbsences = 0;
        private string _qrCodeImageUrl = "qr_placeholder_wireframe.png";
        private string _profileImageUrl;
        public string ProfileImageUrl
        {
            get => _profileImageUrl;
            set
            {
                _profileImageUrl = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasProfileImage));
                OnPropertyChanged(nameof(HasNoProfileImage));
            }
        }

        public bool HasProfileImage => !string.IsNullOrEmpty(ProfileImageUrl);
        public bool HasNoProfileImage => string.IsNullOrEmpty(ProfileImageUrl);

        public string HeaderUsername
        {
            get => _headerUsername;
            set { _headerUsername = value; OnPropertyChanged(); }
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        public string AccountId
        {
            get => _accountId;
            set { _accountId = value; OnPropertyChanged(); }
        }

        public int CountCompleted
        {
            get => _countCompleted;
            set { _countCompleted = value; OnPropertyChanged(); }
        }

        public int CountJoined
        {
            get => _countJoined;
            set { _countJoined = value; OnPropertyChanged(); }
        }

        public int CountAbsences
        {
            get => _countAbsences;
            set { _countAbsences = value; OnPropertyChanged(); }
        }

        public string QrCodeImageUrl
        {
            get => _qrCodeImageUrl;
            set { _qrCodeImageUrl = value; OnPropertyChanged(); }
        }


        

        public V_ProfileViewModel(int accountNum, INavigation navigation, Page page)
        {
            _currentVolunteerAccountNum = accountNum;
            _navigation = navigation;
            _page = page;

            UploadProfilePictureCommand = new Command(async () => await ExecuteUploadProfilePictureAsync());
            DeleteProfilePictureCommand = new Command(async () => await ExecuteDeleteProfilePictureAsync());

            GoHistoryCommand = new Command(async () => await ExecuteGoHistoryAsync());
            GoNotificationsCommand = new Command(async () => await ExecuteGoNotificationsAsync());
            NavigateToHomeCommand = new Command<object>(async (btn) => await ExecuteNavigateToHome(btn));
            NavigateToEventsCommand = new Command<object>(async (btn) => await ExecuteNavigateToEvents(btn));
            NavigateToProposalsCommand = new Command<object>(async (btn) => await ExecuteNavigateToProposals(btn));
            NavigateToProfileCommand = new Command<object>(async (btn) => await ExecuteNavigateToProfile(btn));
            LogoutCommand = new Command(async () => await ExecuteLogoutAsync());

        }

        public async Task LoadProfileDataAsync()
        {
            if (_currentVolunteerAccountNum == 0)
            {
                await _page.DisplayAlert("Error", "Account ID missing. The profile cannot load.", "OK");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. Fetch personal details AND the profile picture URL
                    string profileSql = "SELECT Account_ID, Firstname, Lastname, ProfilePicUrl FROM ACCOUNT WHERE AccountNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(profileSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", _currentVolunteerAccountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string firstName = reader["Firstname"].ToString();
                                string lastName = reader["Lastname"].ToString();

                                HeaderUsername = $"{firstName}!";
                                FullName = $"{firstName} {lastName}".ToUpper();
                                AccountId = reader["Account_ID"].ToString();

                                // Load existing image if they have one
                                ProfileImageUrl = reader["ProfilePicUrl"]?.ToString();

                                QrCodeImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={AccountId}";
                            }
                        }
                    }

                    // 1. Total Events Joined (Total registrations for this volunteer account)
                    string joinedCountSql = "SELECT COUNT(*) FROM EVENTREGISTRATION WHERE AccountNum = @AccNum";
                    using (SqlCommand cmdJoined = new SqlCommand(joinedCountSql, conn))
                    {
                        cmdJoined.Parameters.AddWithValue("@AccNum", _currentVolunteerAccountNum);
                        CountJoined = (int)await cmdJoined.ExecuteScalarAsync();
                    }

                    // 2. Completed / Attended Events (Where AttendanceStatus is 'Present')
                    string completedCountSql = "SELECT COUNT(*) FROM EVENTREGISTRATION WHERE AccountNum = @AccNum AND AttendanceStatus = 'Present'";
                    using (SqlCommand cmdComp = new SqlCommand(completedCountSql, conn))
                    {
                        cmdComp.Parameters.AddWithValue("@AccNum", _currentVolunteerAccountNum);
                        CountCompleted = (int)await cmdComp.ExecuteScalarAsync();
                    }

                    // 3. Absences (Where AttendanceStatus is 'Absent')
                    string absenceCountSql = "SELECT COUNT(*) FROM EVENTREGISTRATION WHERE AccountNum = @AccNum AND AttendanceStatus = 'Absent'";
                    using (SqlCommand cmdAbs = new SqlCommand(absenceCountSql, conn))
                    {
                        cmdAbs.Parameters.AddWithValue("@AccNum", _currentVolunteerAccountNum);
                        CountAbsences = (int)await cmdAbs.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await _page.DisplayAlert("Database Fault", $"Could not load volunteer statistics: {ex.Message}", "OK");
            }
        }

        private async Task ExecuteUploadProfilePictureAsync()
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return;

                using var stream = await photo.OpenReadAsync();
                using var client = new HttpClient();
                using var content = new MultipartFormDataContent();

                var presetContent = new StringContent("effpkvoa");
                presetContent.Headers.ContentType = null;
                content.Add(presetContent, "\"upload_preset\"");

                var fileContent = new StreamContent(stream);
                content.Add(fileContent, "\"file\"", $"\"{photo.FileName}\"");

                string uploadUrl = "https://api.cloudinary.com/v1_1/ahewabql/image/upload";
                var response = await client.PostAsync(uploadUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                    string newImageUrl = jsonDoc.RootElement.GetProperty("secure_url").GetString();

                    using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                    {
                        await conn.OpenAsync();
                        string sql = "UPDATE ACCOUNT SET ProfilePicUrl = @url WHERE AccountNum = @AccNum";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@url", newImageUrl);
                            cmd.Parameters.AddWithValue("@AccNum", _currentVolunteerAccountNum);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    ProfileImageUrl = newImageUrl;
                }
                else
                {
                    string errorText = await response.Content.ReadAsStringAsync();
                    await _page.DisplayAlert("Upload Error", $"Cloudinary refused the upload: {errorText}", "OK");
                }
            }
            catch (Exception ex)
            {
                await _page.DisplayAlert("Error", $"Could not upload image: {ex.Message}", "OK");
            }
        }

        private async Task ExecuteDeleteProfilePictureAsync()
        {
            if (string.IsNullOrEmpty(ProfileImageUrl)) return;

            try
            {
                bool confirm = await _page.DisplayAlert("Delete Photo", "Are you sure you want to remove your profile picture?", "Delete", "Cancel");
                if (!confirm) return;

                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = "UPDATE ACCOUNT SET ProfilePicUrl = NULL WHERE AccountNum = @AccNum";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccNum", _currentVolunteerAccountNum);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                ProfileImageUrl = null;
            }
            catch (Exception ex)
            {
                await _page.DisplayAlert("Error", $"Could not delete image: {ex.Message}", "OK");
            }
        }

        private async Task ExecuteGoBackAsync() => await _navigation.PopAsync();
        private async Task ExecuteGoHistoryAsync() { /* Add your history page navigation here */ }
        private async Task ExecuteGoNotificationsAsync() { /* Add your notification page navigation here */ }
        

        private async Task AnimateButtonAsync(object buttonObj)
        {
            if (buttonObj is ImageButton imgButton)
            {
                await imgButton.ScaleTo(0.92, 50, Easing.Linear);
                await imgButton.ScaleTo(1.0, 50, Easing.Linear);
            }
            else if (buttonObj is Button flatButton)
            {
                await flatButton.ScaleTo(0.92, 50, Easing.Linear);
                await flatButton.ScaleTo(1.0, 50, Easing.Linear);
            }
        }

        private async Task ExecuteNavigateToHome(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_HOME(_currentVolunteerAccountNum));
        }

        private async Task ExecuteNavigateToEvents(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_EVENTS(_currentVolunteerAccountNum)); // Assuming V_EVENTS matches your page name
        }

        private async Task ExecuteNavigateToProposals(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_PROPOSALS(_currentVolunteerAccountNum));
        }

        private async Task ExecuteNavigateToProfile(object buttonObj)
        {
            await AnimateButtonAsync(buttonObj);
            await _navigation.PushAsync(new V_PROFILE(_currentVolunteerAccountNum));
        }

        private async Task ExecuteLogoutAsync()
        {
            bool confirm = await _page.DisplayAlert("Logout Confirmation", "Are you sure you want to exit your session?", "Logout", "Cancel");
            if (confirm)
            {
                await _navigation.PopToRootAsync();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}