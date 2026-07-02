using Handog_MobileApp.ViewModel.Organizer; // Ensure this matches your namespace

namespace Handog_MobileApp
{
    public partial class O_PROPOSALS : ContentPage
    {
        // We declare the ViewModel here so we can access it if needed
        private ProposalsViewModel _viewModel;

        public O_PROPOSALS(int sessionAccountNum)
        {
            InitializeComponent();

            // Link the ViewModel to this Page
            _viewModel = new ProposalsViewModel(sessionAccountNum);
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Trigger the command to load data when the page appears
            _viewModel.LoadDataCommand.Execute(null);
        }
    }
}