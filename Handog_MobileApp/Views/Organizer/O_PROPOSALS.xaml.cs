using Microsoft.Maui.Controls;
using Handog_MobileApp.ViewModel.Organizer;

namespace Handog_MobileApp.Views.Organizer
{
    public partial class O_PROPOSALS : ContentPage
    {
        private ProposalsViewModel _viewModel;

        public O_PROPOSALS(int accountNum)
        {
            InitializeComponent();
            _viewModel = new ProposalsViewModel(accountNum);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // This forces the data to load every time the Organizer opens this page
            if (_viewModel.LoadDataCommand.CanExecute(null))
            {
                await _viewModel.LoadDataCommand.ExecuteAsync(null);
            }
        }
    }
}