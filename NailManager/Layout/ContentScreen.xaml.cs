using System.Windows.Controls;
using NailManager.Helpers;
using NailManager.Layout;

namespace NailManager.Screen
{
    public partial class ContentScreen : UserControl
    {
        public ContentScreen()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private async void InitializeDatabase()
        {
            await DatabaseHelper.InitializeDatabaseAsync();
            CheckLoginStatus();
        }

        private async void CheckLoginStatus()
        {
            var user = await DatabaseHelper.GetUserAsync();
            if (user != null)
            {
                NavigateToHome();
            }
            else
            {
                NavigateToLogin();
            }
        }

        public void NavigateToLogin()
        {
            var loginScreen = new LoginScreen();
            loginScreen.LoginSuccessful += OnLoginSuccessful;
            MainContentControl.Content = loginScreen;
        }

        private void OnLoginSuccessful(object sender, System.EventArgs e)
        {
            NavigateToHome();
        }

        private void NavigateToHome()
        {
            var mainLayout = new MainLayout();
            mainLayout.Logout += OnLogout;
            mainLayout.SetBodyContent(new TabBillCreate());
            MainContentControl.Content = mainLayout;
        }

        private async void OnLogout(object sender, System.EventArgs e)
        {
            var user = await DatabaseHelper.GetUserAsync();
            if (user != null)
            {
                await DatabaseHelper.DeleteUserAsync(user);
            }
            NavigateToLogin();
        }
    }
}