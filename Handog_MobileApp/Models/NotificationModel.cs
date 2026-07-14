using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp.Models
{
    public class NotificationModel : INotifyPropertyChanged
    {
        public int NotificationID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int TargetAccountNum { get; set; }

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set
            {
                _isRead = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BgColor));
                OnPropertyChanged(nameof(TitleFont));
            }
        }

        public string BgColor => IsRead ? "#FFFFFF" : "#F4F9FD";
        public string TitleFont => IsRead ? "None" : "Bold";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}