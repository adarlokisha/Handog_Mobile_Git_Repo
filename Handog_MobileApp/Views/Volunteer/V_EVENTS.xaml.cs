using Handog_MobileApp.ViewModels.Volunteer;

namespace Handog_MobileApp.Views.Volunteer;

public partial class V_EVENTS : ContentPage
{
    private readonly V_EventsViewModel _viewModel;

    public V_EVENTS(int accountNum)
    {
        InitializeComponent();

        _viewModel = new V_EventsViewModel(Navigation, accountNum);
        _viewModel.ShowAlertRequested += OnViewModelShowAlertRequested;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadEventsFromDatabaseAsync();
    }

    private async void OnViewModelShowAlertRequested(string title, string message)
    {
        await DisplayAlert(title, message, "OK");
    }

    private void SetTabActive(Border activeBorder)
    {
        TabMyEvents.BackgroundColor = Colors.Transparent;
        TabAllEvents.BackgroundColor = Colors.Transparent;

        // Uses the page's color profile scheme
        activeBorder.BackgroundColor = Color.FromArgb("#4DD0E1");
    }

    private void MyEventsTab_Tapped(object sender, EventArgs e) => SetTabActive(TabMyEvents);
    private void AllEventsTab_Tapped(object sender, EventArgs e) => SetTabActive(TabAllEvents);
}