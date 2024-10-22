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
            DatabaseHelper.InitializeDatabaseAsync();
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
            try
            {
                LoginLoadingIcon.Visibility = Visibility.Visible;
                string username = UsernameTextBox.Text;
                string password = PasswordBox.Password;

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

                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            LoginRespon? responseData = JsonConvert.DeserializeObject<LoginRespon>(responseBody);
                            DataRespon data = responseData?.data;

                            if (data != null)
                            {
                                string dataAsString = JsonConvert.SerializeObject(data, Formatting.Indented);
                                Console.WriteLine("Response from login");
                                Console.WriteLine(dataAsString);

                                // Sử dụng try-catch trong việc lưu thông tin người dùng
                                try
                                {
                                    // Tạo đối tượng người dùng mới từ dữ liệu đăng nhập
                                    var User = new User
                                    {
                                        UserName = data.user_name,
                                        AccessToken = data.access_token,
                                        Name = data.user_name,
                                        UserId = data.user_id,  // Sử dụng trực tiếp user_id từ API
                                        Permission = data.permision,
                                        BranchId = data.branch_id
                                    };
                                    // string UserString = JsonConvert.SerializeObject(User, Formatting.Indented);
                                    // Console.WriteLine("UserString from login");
                                    // Console.WriteLine(UserString);

                                    // Lưu người dùng mới
                                    await DatabaseHelper.SaveUserAsync(User);

                                    // Gọi sự kiện LoginSuccessful
                                    LoginLoadingIcon.Visibility = Visibility.Collapsed;
                                    LoginSuccessful?.Invoke(this, EventArgs.Empty);
                                }
                                catch (Exception dbEx)
                                {
                                    Console.WriteLine($"Error saving user: {dbEx.Message}");
                                }
                            }
                        }
                        else
                        {
                            ErrorMessageTextBlock.Text = "Invalid username or password.";
                            ErrorMessageTextBlock.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
            }
            finally
            {
                LoginLoadingIcon.Visibility = Visibility.Collapsed;
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