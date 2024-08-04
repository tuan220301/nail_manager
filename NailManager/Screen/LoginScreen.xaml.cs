using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NailManager.Models;
using SharpVectors.Dom.Svg;
using SharpVectors.Renderers.Utils;
using SharpVectors.Renderers.Wpf;

namespace NailManager.Screen
{
    public partial class LoginScreen : UserControl
    {
        private WpfSvgWindow _wpfWindow;
        private bool _isPasswordVisible = false;
        public event EventHandler? LoginSuccessful;

        public LoginScreen()
        {
            InitializeComponent();
            ApplyColors();
            InitializeSvgRendering();
            DisplaySvg();
        }

        private void ApplyColors()
        {
            var colors = new ColorsDefault();
            Application.Current.Resources["BillOrangeColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillOrange));
            Application.Current.Resources["BillMainLightColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillMainLight));
            Application.Current.Resources["BillMainDarkColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillMainDark));
            Application.Current.Resources["BillSecondaryColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillSecondary));
            Application.Current.Resources["BillSandColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillSand));
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

        private async void Submit(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessageTextBlock.Text = "Please fill in both fields.";
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
            }
            else if (username != "user" || password != "user")
            {
                ErrorMessageTextBlock.Text = "Invalid username or password.";
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
                var user = new User { Username = username, Password = password };
                await NailManager.Helpers.DatabaseHelper.SaveUserAsync(user);
                LoginSuccessful?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SubmitKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Submit(sender, e);
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
    }
}
