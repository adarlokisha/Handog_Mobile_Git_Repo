using Handog_MobileApp.ViewModels.UserManagement;

namespace Handog_MobileApp.Views.UserManagement
{
    public partial class SignUpPage : ContentPage
    {
        private readonly SignUpViewModel _viewModel;

        public SignUpPage(string role)
        {
            InitializeComponent();

            _viewModel = new SignUpViewModel(role)
            {
                Navigation = this.Navigation
            };

            BindingContext = _viewModel;

            // Update role label and organizer fields
            RoleLabel.Text = $"You’re signing up as: {role}";
            OrganizerFieldsContainer.IsVisible = role == "Organizer";
        }

        private void OnSignUpClicked(object sender, EventArgs e)
        {
            // Collect values from entries
            _viewModel.FirstName = FirstNameEntry.Text;
            _viewModel.LastName = LastNameEntry.Text;
            _viewModel.Email = EmailEntry.Text;
            _viewModel.Contact = ContactEntry.Text;
            _viewModel.Locale = LocaleEntry.Text;
            _viewModel.Password = PasswordEntry.Text;
            _viewModel.ConfirmPassword = ConfirmPasswordEntry.Text;

            // Execute command
            if (_viewModel.SignUpCommand.CanExecute(null))
            {
                _viewModel.SignUpCommand.Execute(null);
            }
        }
    }
}
