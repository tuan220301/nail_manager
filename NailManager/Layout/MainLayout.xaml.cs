using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using NailManager.Helpers;
using NailManager.Models;
using NailManager.Screen;
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
        private string _currentPage = "Home";

        public MainLayout()
        {
            InitializeComponent();
            Instance = this;
            SetBodyContent(new HomeScreen());
            ApplyColors();
            InitializeSvgRendering();
            DisplaySvg();
            HighlightCurrentPageButton();
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
                case "Home":
                    page = new HomeScreen();
                    break;
                case "Products":
                    page = new ProductsScreen();
                    break;
                // Add cases for other pages
                default:
                    // Handle unknown page case if necessary
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
            foreach (var child in SidebarPanel.Children)
            {
                if (child is Button button)
                {
                    button.ClearValue(Button.BackgroundProperty);
                    if (button.Tag != null && button.Tag.ToString() == _currentPage)
                    {
                        button.Background = (Brush)FindResource("BillOrangeColorBrush");
                    }
                }
            }
        }

        private async void LogoutBtn(object sender, RoutedEventArgs e)
        {
            var user = await DatabaseHelper.GetUserAsync();
            if (user != null)
            {
                await DatabaseHelper.DeleteUserAsync(user);
            }
            Logout?.Invoke(this, EventArgs.Empty);
        }
    }
}
