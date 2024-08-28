using System.Windows;
using System.Windows.Controls;

namespace NailManager.Screen
{
    public partial class AuthenticationWindow : Window
    {
        public string EnteredUsername { get; private set; }
        public string EnteredPassword { get; private set; }
        public bool IsAuthenticated { get; private set; }
        private bool _isPasswordVisible = false;

        public AuthenticationWindow()
        {
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            EnteredUsername = UsernameTextBox.Text;
            EnteredPassword = PasswordBox.Password;

            // Validate the user
            if (ValidateUser(EnteredUsername, EnteredPassword))
            {
                IsAuthenticated = true;
                this.DialogResult = true; // Set the dialog result to true to close the window
            }
            else
            {
                MessageBox.Show("Invalid credentials. Please try again.", "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                IsAuthenticated = false;
            }
        }

        private void ShowPassword(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                TogglePasswordIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.EyeSlash;
                VisiblePassword.Text = PasswordBox.Password;
                VisiblePassword.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                TogglePasswordIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Eye;
                PasswordBox.Password = VisiblePassword.Text;
                VisiblePassword.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
            }
        }

        private bool ValidateUser(string username, string password)
        {
            // Dummy validation logic; replace with actual authentication logic
            return username != "" && password !=""; // Replace with real validation
        }
    }
}
