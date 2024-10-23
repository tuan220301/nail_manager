using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NailManager.Models;
using Newtonsoft.Json;

namespace NailManager.Services
{
    public class Api
    {
        public async Task GetApiAsync(string url, Dictionary<string, string> parameters, Action<string> callback)
        {
            ApiConnect apiConnect = new ApiConnect();
            string apiUrl = apiConnect.Url + url;
            using (HttpClient client = new HttpClient())
            {
                string token = await GetAccessTokenAsync();
                // Console.WriteLine("token: " + token);
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                if (parameters != null && parameters.Count > 0)
                {
                    url += "?" + string.Join("&", parameters.Select(param => $"{param.Key}={param.Value}"));
                }

                try
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // ApiResponse responseConvert = JsonConvert.DeserializeObject<ApiResponse>(responseBody);

                    // Console.WriteLine("Message from API: " + responseConvert.message);  // Truy cập trường message
                    // if (responseConvert.statusCode == 400)
                    // {
                    //     ShowAuthErrorAlert();
                    // }

                    callback(responseBody);
                }
                catch (HttpRequestException e)
                {
                    // if (e.Message.Contains("401") || e.Message.Contains("403") || e.Message.Contains("400"))
                    // {
                    //     if (e.Message.Contains(400.ToString()))
                    //     {
                    //         ShowAuthErrorAlert();
                    //     }
                    //     else
                    //     {
                    //         throw new ArgumentException(
                    //             "Network error. Please check your network connection and try again.", e);
                    //     }
                    // }
                    // else
                    // {
                        Console.WriteLine($"Error call api: {e.Message}");
                    // }
                }
            }
        }

        public async Task PostApiAsync(string url, object parameters, Action<string> callback)
        {
            ApiConnect apiConnect = new ApiConnect();
            string apiUrl = apiConnect.Url + url;
            using (HttpClient client = new HttpClient())
            {
                string token = await GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                try
                {
                    HttpContent content;

                    if (parameters is Dictionary<string, string> dictParameters)
                    {
                        // Nếu parameters là Dictionary<string, string>, sử dụng FormUrlEncodedContent
                        content = new FormUrlEncodedContent(dictParameters);
                    }
                    else if (parameters is string jsonString)
                    {
                        // Nếu parameters là một chuỗi JSON, sử dụng StringContent với JSON
                        content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Invalid parameter type. Only Dictionary<string, string> or JSON string is supported.");
                    }

                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);

// Đọc nội dung phản hồi từ API
                    string responseBody = await response.Content.ReadAsStringAsync();

// In ra JSON từ API (không cần serialize lại)
                    // Console.WriteLine("Response from API (raw JSON):");
                    // Console.WriteLine(responseBody);

// Đảm bảo không có lỗi khi thực hiện yêu cầu
                    response.EnsureSuccessStatusCode();

// Nếu bạn muốn parse JSON từ API thành đối tượng C#
                    // var responseConvert = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
                    // Console.WriteLine("Message from API: " + responseConvert.message);
                    // if (responseConvert.statusCode == 400)
                    // {
                    //     ShowAuthErrorAlert();
                    // }

// Gọi callback với phản hồi ban đầu
                    callback(responseBody);
                }
                catch (HttpRequestException e)
                {
                    // if (e.Message.Contains("401") || e.Message.Contains("403"))
                    // {
                    //     ShowAuthErrorAlert();
                    // }
                    // else
                    // {
                        callback($"Error: {e.Message}");
                    // }
                }
            }
        }
        public async Task PostApiAsyncWithoutParam(string url, Action<string> callback)
        {
            ApiConnect apiConnect = new ApiConnect();
            string apiUrl = apiConnect.Url + url;

            using (HttpClient client = new HttpClient())
            {
                string token = await GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                try
                {
                    // Thực hiện yêu cầu POST mà không có body hay tham số
                    HttpResponseMessage response = await client.PostAsync(apiUrl, null);

                    // Đọc nội dung phản hồi từ API
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Đảm bảo không có lỗi khi thực hiện yêu cầu
                    response.EnsureSuccessStatusCode();

                    // Gọi callback với phản hồi từ API
                    callback(responseBody);
                }
                catch (HttpRequestException e)
                {
                    // Gọi callback với thông báo lỗi
                    callback($"Error: {e.Message}");
                }
            }
        }


        public async Task PostMultipartApiAsync(string url, MultipartFormDataContent content, Action<string> callback)
        {
            ApiConnect apiConnect = new ApiConnect();
            string apiUrl = apiConnect.Url + url;
            using (HttpClient client = new HttpClient())
            {
                string token = await GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                try
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    callback(responseBody);
                }
                catch (HttpRequestException e)
                {
                    // if (e.Message.Contains("401") || e.Message.Contains("403"))
                    // {
                    //     ShowAuthErrorAlert();
                    // }
                    // else
                    // {
                        callback($"Error: {e.Message}");
                    // }
                }
            }
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var user = await DatabaseHelper.GetUserAsync();
            // Console.WriteLine("user.AccessToken: " + user?.AccessToken);
            return user?.AccessToken; // Return null if there's no user
        }

        private void ShowAuthErrorAlert()
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                MessageBoxResult result = MessageBox.Show("Your session has expired. Please log in again.",
                    "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (result == MessageBoxResult.OK)
                {
                    await LogoutAndNavigateToLogin();
                }
            });
        }

        private async Task LogoutAndNavigateToLogin()
        {
            var user = await DatabaseHelper.GetUserAsync();
            if (user != null)
            {
                await DatabaseHelper.DeleteUserAsync(user); // Xóa thông tin người dùng khỏi CSDL
            }

            // Kích hoạt sự kiện đăng xuất để điều hướng về màn hình login
            Application.Current.Dispatcher.Invoke(() =>
            {
                Logout?.Invoke(this, EventArgs.Empty); // Sự kiện Logout được kích hoạt
            });
        }

        public event EventHandler? Logout; // Add this event to trigger navigation to the login page
    }
}