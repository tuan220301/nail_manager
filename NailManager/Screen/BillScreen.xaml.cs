using System.Windows;
using System.Windows.Controls;

namespace NailManager.Screen
{
    public partial class BillScreen : UserControl
    {
        private Button _selectedButton;

        public BillScreen()
        {
            InitializeComponent();
            _selectedButton = CreateButton; // Mặc định chọn nút Create
            UpdateDynamicContent("Create");
        }

        private void TabChange(object sender, RoutedEventArgs e)
        {
            if (_selectedButton != null)
            {
                _selectedButton.Style = (Style)FindResource("UnselectedButtonStyle");
            }

            _selectedButton = sender as Button;
            _selectedButton.Style = (Style)FindResource("SelectedButtonStyle");

            string selectedTag = _selectedButton.Tag.ToString();
            UpdateDynamicContent(selectedTag);
        }

        private void UpdateDynamicContent(string tag)
        {
            UserControl content = null;
            switch (tag)
            {
                case "Create":
                    content = new TabBillCreate(); // Thay thế bằng UserControl tương ứng
                    break;
                case "List":
                    content = new TabBillList(); // Thay thế bằng UserControl tương ứng
                    break;
                default:
                    content = new TabBillCreate(); // Thay thế bằng UserControl tương ứng
                    break;
            }

            DynamicContent.Content = content;
        }
    }
}