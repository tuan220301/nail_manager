using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NailManager.Models;
using SharpVectors.Dom.Svg;
using SharpVectors.Renderers.Utils;
using SharpVectors.Renderers.Wpf;
using System.Net.Http;
using System.Text;
using NailManager.Layout;
using NailManager.Services;
using Newtonsoft.Json;
using static System.Int32;

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
            Application.Current.Resources["BillOrangeColor"] =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillOrange));
            Application.Current.Resources["BillMainLightColor"] =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillMainLight));
            Application.Current.Resources["BillMainDarkColor"] =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillMainDark));
            Application.Current.Resources["BillSecondaryColor"] =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillSecondary));
            Application.Current.Resources["BillSandColor"] =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.BillSand));
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
            LoginLoadingIcon.Visibility = Visibility.Visible;
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // Log giá trị username và password
            Console.WriteLine($"username: {username}");
            Console.WriteLine($"password: {password}");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessageTextBlock.Text = "Please fill in both fields.";
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
                LoginLoadingIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                var user = new { user_name = username, password = password };

                string json = JsonConvert.SerializeObject(user);
                ApiConnect apiString = new ApiConnect();
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"{apiString.Url}/auth/login", content);

                    // Log response
                    // Console.WriteLine($"response: {response.ToString()}");

                    if (response.IsSuccessStatusCode)
                        // if(user.user_name == "user" && password == "user")
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        LoginRespon? responseData = JsonConvert.DeserializeObject<LoginRespon>(responseBody);

                        // Log responseData
                        // Console.WriteLine($"responseData: {responseData}");
                        // Console.WriteLine("Status: " + responseData.status);
                        if (response.StatusCode == (HttpStatusCode)200)
                        {
                            // Console.WriteLine("Message: " + responseData.message);
                            // Console.WriteLine("Message: " + responseData.data.access_token);
                            DataRespon data = responseData.data;
                            // Save user to database if needed
                            await DatabaseHelper.SaveUserAsync(new User
                            {
                                UserName = data.user_name,
                                AccessToken = data.access_token,
                                Name = data.user_name,
                                UserId = Parse(data.user_id),
                                Permission = data.permision,
                                BranchId = data.branch_id
                            });
                        }

                        LoginLoadingIcon.Visibility = Visibility.Collapsed;
                        LoginSuccessful?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        ErrorMessageTextBlock.Text = "Invalid username or password.";
                        ErrorMessageTextBlock.Visibility = Visibility.Visible;
                        LoginLoadingIcon.Visibility = Visibility.Collapsed;
                    }
                }
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