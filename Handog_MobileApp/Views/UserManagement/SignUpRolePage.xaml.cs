using Handog_MobileApp.ViewModels.UserManagement;

namespace Handog_MobileApp.Views.UserManagement
{
    public partial class SignUpRolePage : ContentPage
    {
        public SignUpRolePage()
        {
            InitializeComponent();
            BindingContext = new SignUpRoleViewModel { Navigation = this.Navigation };
        }
    }
}
