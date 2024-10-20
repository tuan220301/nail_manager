using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NailManager.Models;
using NailManager.Screen;
using NailManager.Services;
using SharpVectors.Dom.Svg;
using SharpVectors.Renderers.Utils;
using SharpVectors.Renderers.Wpf;

namespace NailManager.Layout
{
    public partial class MainLayout : UserControl
    {
        public static MainLayout? Instance { get; private set; }
        private WpfSvgWindow _wpfWindow;
        public event EventHandler? Logout;
        private string _currentPage = "New";
        public string permision = "";

        public MainLayout()
        {
            InitializeComponent();
            Instance = this;
            SetBodyContent(new TabBillCreate());
            ApplyColors();
            InitializeSvgRendering();
            DisplaySvg();
            HighlightCurrentPageButton();
            GetUser();
        }

        private void ApplyColors()
        {
            var colors = new ColorsDefault();
            Application.Current.Resources["BillOrangeColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillOrange));
            Application.Current.Resources["BillMainLightColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillMainLight));
            Application.Current.Resources["BillMainDarkColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillMainDark));
            Application.Current.Resources["BillSecondaryColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillSecondary));
            Application.Current.Resources["BillSandColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillSand));
            Application.Current.Resources["BillMainRed"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillMainRed));
        }
        private async void GetUser ()
        {
            ShowLoading(true);
            var user = await DatabaseHelper.GetUserAsync();
            if (user != null)
            {
                userName.Text = user.Name;
                permision = user.Permission;
                CheckPermissionsAndShowMenu(permision);
                ShowLoading(false);
            }
            
        }
        private void CheckPermissionsAndShowMenu(string permission)
        {
            // Ẩn toàn bộ các menu trước
            foreach (var child in SidebarPanel.Children)
            {
                if (child is Button btn)
                {
                    if (btn.Tag.ToString() == "Employee")
                    {
                        btn.Visibility = Visibility.Visible; // Nút Employee luôn hiển thị
                    }
                    else
                    {
                        btn.Visibility = Visibility.Collapsed; // Ẩn các nút khác
                    }
                }
            }

            // Kiểm tra quyền và hiển thị menu tương ứng
            if (permission.Contains("1"))
            {
                // AdminButton.Visibility = Visibility.Visible;
                // Hiển thị tất cả menu
                foreach (var child in SidebarPanel.Children)
                {
                    if (child is Button btn)
                    {
                        btn.Visibility = Visibility.Visible;
                    }
                }
            }

            // else
            // {
            //     AdminButton.Visibility = Visibility.Collapsed;
            // }
        }
        private void ShowMenuByTag(string tag)
        {
            foreach (var child in SidebarPanel.Children)
            {
                if (child is Button btn && btn.Tag?.ToString() == tag)
                {
                    btn.Visibility = Visibility.Visible;
                    break;
                }
            }
        }

        private void InitializeSvgRendering()
        {
            var wpfSettings = new WpfDrawingSettings();
            var wpfRenderer = new WpfDrawingRenderer(wpfSettings);
            _wpfWindow = new WpfSvgWindow(500, 500, wpfRenderer);
        }

        private void DisplaySvg()
        {
            var drawing = RenderSvg("Images/Svgs/logo.svg");
            if (drawing != null)
            {
                var drawingImage = new DrawingImage(drawing);
                LogoSvg.Source = drawingImage;
            }
            else
            {
                MessageBox.Show("SVG could not be loaded.");
            }
        }

        private DrawingGroup RenderSvg(string svgFile)
        {
            try
            {
                var wpfSettings = new WpfDrawingSettings();
                var wpfRenderer = new WpfDrawingRenderer(wpfSettings);

                var drawingDocument = new WpfDrawingDocument();
                wpfRenderer.BeginRender(drawingDocument);
                _wpfWindow.LoadDocument(svgFile, wpfSettings);
                var svgDocument = _wpfWindow.Document as SvgDocument;
                wpfRenderer.Render(svgDocument);
                var drawing = wpfRenderer.Drawing;
                wpfRenderer.EndRender();

                return drawing;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading SVG: {ex.Message}");
                return null;
            }
        }

        public void ShowToast(string message)
        {
            ToastNotification.Visibility = Visibility.Visible;
            // Set the toast message here if needed
        }

        public void ShowLoading(bool show)
        {
            LoadingIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetBodyContent(UserControl control)
        {
            BodyContent.Content = control;
        }

        private void NavigateBtnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                // Reset tất cả các nút về UnselectedButtonStyle trước
                foreach (var child in SidebarPanel.Children)
                {
                    if (child is Button btn)
                    {
                        btn.Style = (Style)FindResource("UnselectedButtonStyle");
                    }
                }

                // Đặt style của nút đã nhấn thành SelectedButtonStyle
                button.Style = (Style)FindResource("SelectedButtonStyle");

                // Điều hướng đến trang tương ứng
                var pageName = button.Tag.ToString();
                NavigateToPage(pageName);
            }
        }

        private void NavigateToPage(string? pageName)
        {
            UserControl page = null!;
            _currentPage = pageName;
            switch (pageName)
            {
                case "New":
                    page = new TabBillCreate();
                    break;
                case "Process":
                    page = new TabBillList();
                    break;
                case "Products":
                    page = new ProductsScreen();
                    break;
                case "Employee":
                    page = new EmployeeScreen();
                    break;
                case "Admin":
                    page = new AdminScreen();
                    break;
                default:
                    page = new TabBillCreate();
                    break;
            }

            if (page != null)
            {
                SetBodyContent(page);
                HighlightCurrentPageButton();
            }
        }

        private void HighlightCurrentPageButton()
        {
            // Kiểm tra các nút trong SidebarPanel
            foreach (var child in SidebarPanel.Children)
            {
                if (child is Button button)
                {
                    if (button.Tag != null && button.Tag.ToString() == _currentPage)
                    {
                        button.Style = (Style)FindResource("SelectedButtonStyle");
                    }
                    else
                    {
                        button.Style = (Style)FindResource("UnselectedButtonStyle");
                    }
                }
            }

            // Kiểm tra các nút trong HeaderPanel
            foreach (var child in HeaderPanel.Children)
            {
                if (child is Button button)
                {
                    if (button.Tag != null && button.Tag.ToString() == _currentPage)
                    {
                        button.Style = (Style)FindResource("SelectedButtonStyle");
                    }
                    else
                    {
                        button.Style = (Style)FindResource("UnselectedButtonStyle");
                    }
                }
            }
        }


        private async void LogoutBtn(object sender, RoutedEventArgs e)
        {
            ShowLoading(true);
            var user = await DatabaseHelper.GetUserAsync();
            if (user != null)
            {
                Console.WriteLine("user is exited");
                await DatabaseHelper.DeleteUserAsync(user);
            }
            Logout?.Invoke(this, EventArgs.Empty);
            ShowLoading(false);
        }
    }
}
