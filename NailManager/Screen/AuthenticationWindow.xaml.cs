using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NailManager.Screen
{
    public partial class AuthenticationWindow : Window
    {
        public string EnteredUsername { get; private set; }
        public string EnteredPassword { get; private set; }
        public bool IsAuthenticated { get; private set; }
        private bool _isPasswordVisible = false;
        public string UserPermission { get; private set; }

        public AuthenticationWindow()
        {
            InitializeComponent();
            PermissionComboBox.SelectedItem = PermissionComboBox.Items[0];
        }

        private void Submit(object sender, RoutedEventArgs e)
        {
            EnteredPassword = PasswordBox.Password;

            if (PermissionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedPermission = selectedItem.Content.ToString();
                UserPermission = selectedPermission;

                // Kiểm tra đầu vào dựa trên loại quyền
                if (selectedPermission == "Branch")
                {
                    EnteredUsername = UsernameGrid.Text;
                    if (string.IsNullOrEmpty(EnteredUsername) || string.IsNullOrEmpty(EnteredPassword))
                    {
                        MessageBox.Show("Please enter both Username and Password for Branch.", "Input Required",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        IsAuthenticated = false;
                        return;
                    }
                }
                else if (selectedPermission == "STAFF")
                {
                    if (string.IsNullOrEmpty(EnteredPassword))
                    {
                        MessageBox.Show("Please enter the Password for Staff.", "Input Required", MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        IsAuthenticated = false;
                        return;
                    }
                }

                IsAuthenticated = true;
                this.DialogResult = true;
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

        private void SubmitKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Submit(sender, e);
            }
        }

        private bool ValidateUser(string username, string password)
        {
            // Dummy validation logic; replace with actual authentication logic
            return username != "" && password != ""; // Replace with real validation
        }

        private void PermissionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PermissionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString() == "Branch")
                {
                    UsernameGrid.Visibility = Visibility.Visible;
                    UsernameTextBlock.Visibility = Visibility.Visible;
                }
                else // "STAFF"
                {
                    UsernameTextBlock.Visibility = Visibility.Collapsed;
                    UsernameGrid.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}