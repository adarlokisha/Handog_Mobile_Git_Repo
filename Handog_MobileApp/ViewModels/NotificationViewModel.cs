using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Handog_MobileApp.Services;
using Handog_MobileApp.ViewModels.Organizer; // <-- Added this to find your AppNotification class

namespace Handog_MobileApp.ViewModels
{
    public class NotificationViewModel : INotifyPropertyChanged
    {
        protected readonly NotificationService _notificationService = new();

        public ICommand ViewNotificationsCommand { get; }
        public ICommand NotificationTappedCommand { get; }

        // 1. Changed from NotificationModel to AppNotification
        public ObservableCollection<AppNotification> Notifications { get; } = new();

        private bool _isNotificationPanelVisible;
        public bool IsNotificationPanelVisible
        {
            get => _isNotificationPanelVisible;
            set { _isNotificationPanelVisible = value; OnPropertyChanged(); }
        }

        private bool _hasUnreadNotifications;
        public bool HasUnreadNotifications
        {
            get => _hasUnreadNotifications;
            set { _hasUnreadNotifications = value; OnPropertyChanged(); }
        }

        public NotificationViewModel()
        {
            ViewNotificationsCommand = new Command(() => IsNotificationPanelVisible = !IsNotificationPanelVisible);

            // 2. Changed type parameter to AppNotification
            NotificationTappedCommand = new Command<AppNotification>(async (noti) => await ProcessNotificationTap(noti));
        }

        public async Task RefreshNotificationsAsync(int accountNum)
        {
            var data = await _notificationService.GetNotificationsAsync(accountNum);
            Notifications.Clear();

            bool unreadFound = false;
            foreach (var item in data)
            {
                Notifications.Add(item); // This will compile perfectly now!
                if (!item.IsRead) unreadFound = true;
            }

            HasUnreadNotifications = unreadFound;
        }

        // 3. Changed parameter type to AppNotification
        private async Task ProcessNotificationTap(AppNotification notification)
        {
            if (notification == null) return;

            notification.IsRead = true;
            IsNotificationPanelVisible = false;
            HasUnreadNotifications = Notifications.Any(n => !n.IsRead);

            // 4. Fixed casing to match your AppNotification property: NotificationID
            await _notificationService.MarkAsReadAsync(notification.NotificationID);
            await Application.Current.MainPage.DisplayAlert(notification.Title, notification.Message, "OK");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}