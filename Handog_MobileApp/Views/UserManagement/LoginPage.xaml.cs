using Microsoft.Maui.Controls;
using Handog_MobileApp.ViewModels.UserManagement;

namespace Handog_MobileApp.Views.UserManagement
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();

            BindingContext = new LoginViewModel { Navigation = this.Navigation };
        }
    }
}