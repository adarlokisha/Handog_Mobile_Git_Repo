using Microsoft.Maui.Controls;
using Handog_MobileApp.Models;
using Handog_MobileApp.ViewModels.Organizer;

namespace Handog_MobileApp.Views.Organizer
{
    public partial class O_REPORT : ContentPage
    {
        // We require the EventModel to be passed in so we know which report to load
        public O_REPORT(EventModel completedEvent)
        {
            InitializeComponent();

            // Connect the XAML page to the ViewModel, passing the event data straight through
            BindingContext = new ReportsViewModel(completedEvent);
        }
    }
}