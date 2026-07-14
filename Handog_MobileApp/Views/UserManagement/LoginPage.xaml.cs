using Handog_MobileApp.ViewModels.UserManagement;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp.Views.UserManagement
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            BindingContext = new ViewModels.UserManagement.LoginViewModel { Navigation = this.Navigation };
        }
    }
}
