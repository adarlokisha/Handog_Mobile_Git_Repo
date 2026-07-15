using System.Windows.Input;
using Microsoft.Maui.Controls;
using Handog_MobileApp.Views.UserManagement;
using Handog_MobileApp.Models;


namespace Handog_MobileApp.ViewModels.UserManagement
{
    public class SignUpRoleViewModel : BindableObject
    {
        private string _selectedRole;
        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolunteerBackgroundColor));
                OnPropertyChanged(nameof(VolunteerStrokeColor));
                OnPropertyChanged(nameof(VolunteerStrokeThickness));
                OnPropertyChanged(nameof(VolunteerTextColor));
                OnPropertyChanged(nameof(OrganizerBackgroundColor));
                OnPropertyChanged(nameof(OrganizerStrokeColor));
                OnPropertyChanged(nameof(OrganizerStrokeThickness));
                OnPropertyChanged(nameof(OrganizerTextColor));
            }
        }

        // Volunteer styles
        public Color VolunteerBackgroundColor => SelectedRole == "Volunteer" ? Colors.LightBlue : Colors.Transparent;
        public Color VolunteerStrokeColor => SelectedRole == "Volunteer" ? Colors.Black : Colors.Black;
        public double VolunteerStrokeThickness => SelectedRole == "Volunteer" ? 2 : 1;
        public Color VolunteerTextColor => SelectedRole == "Volunteer" ? Colors.Black : Colors.Black;

        // Organizer styles
        public Color OrganizerBackgroundColor => SelectedRole == "Organizer" ? Colors.LightBlue : Colors.Transparent;
        public Color OrganizerStrokeColor => SelectedRole == "Organizer" ? Colors.Black : Colors.Gray;
        public double OrganizerStrokeThickness => SelectedRole == "Organizer" ? 2 : 1;
        public Color OrganizerTextColor => SelectedRole == "Organizer" ? Colors.Black : Colors.Black;

        public ICommand SelectVolunteerCommand { get; }
        public ICommand SelectOrganizerCommand { get; }
        public ICommand ContinueCommand { get; }

        public INavigation Navigation { get; set; }

        public SignUpRoleViewModel()
        {
            SelectVolunteerCommand = new Command(() => SelectedRole = "Volunteer");
            SelectOrganizerCommand = new Command(() => SelectedRole = "Organizer");

            ContinueCommand = new Command(async () =>
            {
                if (string.IsNullOrEmpty(SelectedRole))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Please select a role before continuing.", "OK");
                    return;
                }

                await Navigation.PushAsync(new SignUpPage(SelectedRole));
            });
        }
    }
}
