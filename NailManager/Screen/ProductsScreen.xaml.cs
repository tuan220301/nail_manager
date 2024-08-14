using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using NailManager.Layout;
using NailManager.Models;
using NailManager.Services;
using Newtonsoft.Json;

namespace NailManager.Screen;

public partial class ProductsScreen : UserControl
{
    public ObservableCollection<Product> Products { get; set; }
    public ICollectionView FilteredProducts { get; set; }

    public ProductsScreen()
    {
        InitializeComponent();
        Products = new ObservableCollection<Product>();
        FilteredProducts = CollectionViewSource.GetDefaultView(Products);
        DataContext = this;
        GetBrandList();
        GetListProduct();
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
    private void BranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BranchComboBox.SelectedValue != null)
        {
            int selectedBranchId = (int)BranchComboBox.SelectedValue;
            GetListProduct(selectedBranchId);  // Gọi lại với branch_id mới
        }
    }

    private async void GetListProduct(int branchId = 1)
    {
        ShowLoading(true); // Hiển thị loading

        string url = $"/product/list?branch_id={branchId}";
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

                var responseData = JsonConvert.DeserializeObject<BranchApiResponse<List<Product>>>(responseBody);

                if (responseData == null)
                {
                    MessageBox.Show("Failed to parse API response.");
                    return;
                }

                if (responseData.status == 200 && responseData.data != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Products.Clear();  // Xóa dữ liệu cũ
                        foreach (var product in responseData.data)
                        {
                            Products.Add(product);
                        }

                        FilteredProducts.Refresh(); // Làm mới CollectionView để cập nhật UI
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


    private async void OnSubmit(object sender, RoutedEventArgs e)
    {
        ShowLoading(true); // Hiển thị loading
        try
        {
            // Retrieve input values
            string name = Name.Text;
            string amount = Amout.Text;
            int branchId = (int)BranchComboBox.SelectedValue;

            // Define the API endpoint URL
            string url = "/product/create"; // Replace with your actual API endpoint
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            // Add the parameters to the dictionary
            parameters.Add("product_name", name);
            parameters.Add("price", amount);
            parameters.Add("branch_id", branchId.ToString());

            // Create an instance of the Api class to use its PostApiAsync method
            var apiService = new Api();
            await apiService.PostApiAsync(url, parameters, (responseBody) =>
            {
                try
                {
                    // Assuming the response is JSON, deserialize it to check the status or get data
                    var responseData =
                        JsonConvert.DeserializeObject<BranchApiResponse<CreateProductResponse>>(responseBody);

                    // Check if the response indicates success
                    if (responseData != null && responseData.status == 200)
                    {
                        // MessageBox.Show("Product successfully added to the branch.");
                        GetListProduct(branchId);
                    }
                    else
                    {
                        MessageBox.Show($"API Error: {responseData?.message ?? "Unknown error"}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing API response: {ex.Message}");
                }
            });
        }
        finally
        {
            ShowLoading(false); // Ẩn loading khi hoàn tất
        }
    }

    private void ProductItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is Product selectedProduct)
        {
            // Điền thông tin của sản phẩm vào các trường input
            Name.Text = selectedProduct.product_name;
            Amout.Text = selectedProduct.price.ToString();
            // Các trường khác nếu cần
        }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Xóa toàn bộ dữ liệu trong các trường input
        Name.Text = string.Empty;
        Amout.Text = string.Empty;
        // Xóa các trường khác nếu cần
    }
    private void ShowLoading(bool show)
    {
        Dispatcher.Invoke(() =>
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        });
    }
}
