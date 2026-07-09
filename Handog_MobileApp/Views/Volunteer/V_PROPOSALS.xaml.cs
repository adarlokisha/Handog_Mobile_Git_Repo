using System;
using Handog_MobileApp.ViewModels.Volunteer;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp
{
    public partial class V_PROPOSALS : ContentPage
    {
        private readonly V_ProposalsViewModel _viewModel;

        public V_PROPOSALS(int accountNum)
        {
            InitializeComponent();

            _viewModel = new V_ProposalsViewModel(Navigation, accountNum);
            _viewModel.ShowAlertRequested += OnViewModelShowAlertRequested;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadHeaderUsernameAsync();
            await _viewModel.LoadProposalsFromDatabaseAsync();
        }

        private async void OnViewModelShowAlertRequested(string title, string message)
        {
            await DisplayAlert(title, message, "OK");
        }

        private void SetTabActive(Border activeBorder)
        {
            TabMyProposals.BackgroundColor = Colors.Transparent;
            TabAllProposals.BackgroundColor = Colors.Transparent;
            TabApprovedProposals.BackgroundColor = Colors.Transparent;
            activeBorder.BackgroundColor = Colors.White;
        }

        private void MyProposalsTab_Tapped(object sender, EventArgs e)
        {
            SetTabActive(TabMyProposals);
            _viewModel.CurrentTab = "MyProposals";
            _ = _viewModel.LoadProposalsFromDatabaseAsync();
        }

        private void AllProposalsTab_Tapped(object sender, EventArgs e)
        {
            SetTabActive(TabAllProposals);
            _viewModel.CurrentTab = "AllProposals";
            _ = _viewModel.LoadProposalsFromDatabaseAsync();
        }

        private void ApprovedProposalsTab_Tapped(object sender, EventArgs e)
        {
            SetTabActive(TabApprovedProposals);
            _viewModel.CurrentTab = "ApprovedProposals";
            _ = _viewModel.LoadProposalsFromDatabaseAsync();
        }
    }
}