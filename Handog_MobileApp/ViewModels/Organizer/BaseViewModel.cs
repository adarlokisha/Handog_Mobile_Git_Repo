using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp
{
    // Make sure you inherit from ObservableObject to use [RelayCommand]
    public partial class BaseViewModel : ObservableObject
    {
        protected int _currentAccountNum;

        // Navigation Commands
        [RelayCommand]
        public async Task GoHome() => await Shell.Current.Navigation.PushAsync(new O_HOME(_currentAccountNum));

        [RelayCommand]
        public async Task GoProposals() => await Shell.Current.Navigation.PushAsync(new O_PROPOSALS(_currentAccountNum));

        [RelayCommand]
        public async Task GoEvents() => await Shell.Current.Navigation.PushAsync(new O_EVENTS(_currentAccountNum));

        [RelayCommand]
        public async Task GoProfile() => await Shell.Current.Navigation.PushAsync(new O_PROFILE(_currentAccountNum));
    }
}