using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using NailManager.Models;
using NailManager.Services;
using Newtonsoft.Json;

namespace NailManager.Screen;

public partial class EmployeeScreen : UserControl
{
    public ObservableCollection<User> Users { get; set; }
    public ICollectionView FilteredUsers { get; set; }
    public EmployeeScreen()
    {
        InitializeComponent();
        FilteredUsers = CollectionViewSource.GetDefaultView(Users);
        DataContext = this;
        GetBrandList();
        GetListUser();
        
    }
    private async void GetBrandList()
    {
        ShowLoading(true); // Hiển thị loading
        string url = "/branch/list"; // Replace with your API endpoint
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        var apiService = new Api(); // Create an instance of the class containing GetApiAsync

        await apiService.GetApiAsync(url, parameters, (responseBody) =>
        {
            try
            {
                var responseData = JsonConvert.DeserializeObject<BranchApiResponse<List<Branch>>>(responseBody);

                if (responseData != null && responseData.status == 200)
                {
                    Dispatcher.Invoke(() =>
                    {
                        BranchComboBox.ItemsSource = responseData.data;
                        BranchComboBox.DisplayMemberPath = "name";
                        BranchComboBox.SelectedValuePath = "branch_id";
                        BranchComboBox.SelectedValue = 1; // Đặt giá trị mặc định là branch_id = 1
                    });
                }
                else
                {
                    MessageBox.Show("Failed to load branches.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing API response: {ex.Message}");
            }
            finally
            {
                ShowLoading(false); // Ẩn loading khi hoàn tất
            }
        });
    }
    private async void GetListUser(int branchId = 1)
    {
        ShowLoading(true); // Hiển thị loading

        string url = $"/user/list?branch_id={branchId}";
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        var apiService = new Api();

        await apiService.GetApiAsync(url, parameters, (responseBody) =>
        {
            try
            {
                if (string.IsNullOrEmpty(responseBody))
                {
                    MessageBox.Show("API response is empty or null.");
                    return;
                }

                var responseData = JsonConvert.DeserializeObject<BranchApiResponse<List<User>>>(responseBody);

                if (responseData == null)
                {
                    MessageBox.Show("Failed to parse API response.");
                    return;
                }

                if (responseData.status == 200 && responseData.data != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Users.Clear();  // Xóa dữ liệu cũ
                        foreach (var user in responseData.data)
                        {
                            Users.Add(user);
                        }

                        FilteredUsers.Refresh(); // Làm mới CollectionView để cập nhật UI
                    });
                }
                else
                {
                    MessageBox.Show("Failed to load products.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing API response: {ex.Message}");
            }
            finally
            {
                ShowLoading(false); // Ẩn loading khi hoàn tất
            }
        });
    }
    private void BranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BranchComboBox.SelectedValue != null)
        {
            int selectedBranchId = (int)BranchComboBox.SelectedValue;
            GetListUser(selectedBranchId);  // Gọi lại với branch_id mới
        }
    }
    private void ShowLoading(bool show)
    {
        Dispatcher.Invoke(() =>
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        });
    }
}