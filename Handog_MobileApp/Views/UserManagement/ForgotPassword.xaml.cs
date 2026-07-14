using Handog_MobileApp.ViewModels.UserManagement;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp.Views.UserManagement
{
    public partial class ForgotPasswordPage : ContentPage
    {
        public ForgotPasswordPage()
        {
            InitializeComponent();
            BindingContext = new ForgotPasswordViewModel { Navigation = this.Navigation };
        }
    }
}