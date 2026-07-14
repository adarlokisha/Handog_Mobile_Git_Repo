using Handog_MobileApp.ViewModels.UserManagement;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp.Views.UserManagement
{
    public partial class SignUpVerificationPage : ContentPage
    {
        public SignUpVerificationPage(string email, string code)
        {
            InitializeComponent();
            BindingContext = new SignUpVerificationViewModel(email, code) { Navigation = this.Navigation };
        }
    }
}
