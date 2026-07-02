using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using Handog_MobileApp.Models;

namespace Handog_MobileApp
{
    public partial class O_EVENTS : ContentPage
    {
        private readonly EventsViewModel _viewModel;

        // We add the proposal here as an OPTIONAL parameter (defaults to null)
        public O_EVENTS(int sessionAccountNum, O_EventProposal proposal = null)
        {
            InitializeComponent();

            _viewModel = new EventsViewModel(sessionAccountNum);
            BindingContext = _viewModel;

            // If a proposal was passed in, immediately load it into the form
            if (proposal != null)
            {
                _viewModel.LoadProposalIntoForm(proposal);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}