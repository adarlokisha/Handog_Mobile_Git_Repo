using Handog_MobileApp.ViewModels;
using Handog_MobileApp.ViewModels.Volunteer;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp;

public partial class V_HOME : ContentPage
{
    private readonly V_HomeViewModel _viewModel;

    public V_HOME(int accountNum)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);

        // Bind layout life scope context to view model pipeline
        _viewModel = new V_HomeViewModel(Navigation, accountNum);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Safely trigger asynchronous network tasks outside thread lock contexts
        if (_viewModel != null)
        {
            await _viewModel.InitializeAsync();
        }
    }
}