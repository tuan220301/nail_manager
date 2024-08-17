using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NailManager.Layout;
using NailManager.Models;
using NailManager.Services;
using Newtonsoft.Json;

namespace NailManager.Screen;

public partial class ProductsScreen : UserControl
{
    public ObservableCollection<Product> Products { get; set; }
    public ICollectionView FilteredProducts { get; set; }

    public int BranchId { get; set; }
    private ApiConnect _apiConnect = new ApiConnect();

    // Biến để lưu trữ sản phẩm đang được chỉnh sửa
    private Product _selectedProduct;
    private bool isNewImageSelected = false;
    public ProductsScreen()
    {
        InitializeComponent();
        Products = new ObservableCollection<Product>();
        FilteredProducts = CollectionViewSource.GetDefaultView(Products);
        DataContext = this;
        GetUser();
        GetBrandList();
        GetListProduct();
    }

    private async void GetUser()
    {
        ShowLoading(true);
        var user = await DatabaseHelper.GetUserAsync();
        if (user != null)
        {
            if (user.BranchId > 0)
            {
                BranchId = user.BranchId;
                FilterBranch.Visibility = Visibility.Collapsed;
            }
            else
            {
                FilterBranch.Visibility = Visibility.Visible;
            }

            ShowLoading(false);
        }
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
            GetListProduct(selectedBranchId); // Gọi lại với branch_id mới
        }
    }

    private async void GetListProduct(int branchId = 1)
    {
        ShowLoading(true); // Hiển thị loading
        if (BranchId > 0)
        {
            branchId = BranchId;
        }

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
                        Products.Clear(); // Xóa dữ liệu cũ
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
        string name = Name.Text;
        string amount = Amout.Text;
        int branchId = (int)BranchComboBox.SelectedValue;

        var client = new HttpClient();
        HttpRequestMessage request;
        MultipartFormDataContent content = new MultipartFormDataContent();

        // Kiểm tra nếu đang ở trạng thái chỉnh sửa
        if (_selectedProduct != null)
        {
            // Chỉnh sửa sản phẩm
            content.Add(new StringContent(name), "product_name");
            content.Add(new StringContent(_selectedProduct.product_id.ToString()), "product_id");
            content.Add(new StringContent(amount), "price");
            content.Add(new StringContent(branchId.ToString()), "branch_id");

            if (isNewImageSelected) // Chỉ gọi API with-media khi có ảnh mới
            {
                Console.WriteLine("update with image");
                request = new HttpRequestMessage(HttpMethod.Post, $"{_apiConnect.Url}/product/update-with-media");

                // Lấy đường dẫn hình ảnh từ nguồn ảnh
                var imagePath = ((BitmapImage)SelectedImage.Source).UriSource.LocalPath;
                content.Add(new StreamContent(File.OpenRead(imagePath)), "image", Path.GetFileName(imagePath));
            }
            else
            {
                Console.WriteLine("update without image");
                request = new HttpRequestMessage(HttpMethod.Post, $"{_apiConnect.Url}/product/update-no-media");
            }
        }
        else
        {
            // Thêm sản phẩm mới
            request = new HttpRequestMessage(HttpMethod.Post, $"{_apiConnect.Url}/product/create-new");
            content.Add(new StringContent(name), "product_name");
            content.Add(new StringContent(amount), "price");
            content.Add(new StringContent(branchId.ToString()), "branch_id");

            if (SelectedImage.Source != null && SelectedImage.Visibility == Visibility.Visible)
            {
                var imagePath = ((BitmapImage)SelectedImage.Source).UriSource.LocalPath;
                content.Add(new StreamContent(File.OpenRead(imagePath)), "image", Path.GetFileName(imagePath));
            }
        }

        request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        // Xử lý phản hồi từ API
        var responseData = JsonConvert.DeserializeObject<BranchApiResponse<CreateProductResponse>>(responseBody);
        if (responseData != null && responseData.status == 200)
        {
            GetListProduct(branchId);
            Cancel_Click(null, null); // Xóa dữ liệu trên các ô nhập
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
    finally
    {
        ShowLoading(false); // Ẩn loading khi hoàn tất
    }
}

    private async void ProductItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is Product selectedProduct)
        {
            // Điền thông tin của sản phẩm vào các trường input
            Name.Text = selectedProduct.product_name;
            Amout.Text = selectedProduct.price.ToString();

            // Lưu trữ sản phẩm hiện tại để chỉnh sửa
            _selectedProduct = selectedProduct;

            // Đổi nội dung nút thành "Edit product"
            SubmitButton.Content = "Edit product";

            // Hiển thị trạng thái "Loading..." và ẩn hình ảnh cũ
            LoadingText.Visibility = Visibility.Visible;
            SelectedImage.Visibility = Visibility.Collapsed;
            ImageIcon.Visibility = Visibility.Collapsed;

            // Tải và hiển thị hình ảnh từ URL
            if (!string.IsNullOrEmpty(selectedProduct.url_image))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(selectedProduct.url_image, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;

                    // Event when the image is fully loaded
                    bitmap.DownloadCompleted += (s, ev) =>
                    {
                        // Sau khi tải xong, cập nhật UI trên luồng chính
                        SelectedImage.Source = bitmap;
                        SelectedImage.Visibility = Visibility.Visible;
                        LoadingText.Visibility = Visibility.Collapsed; // Ẩn "Loading..."
                    };

                    bitmap.EndInit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}");
                    LoadingText.Visibility = Visibility.Collapsed; // Ẩn "Loading..." nếu có lỗi
                    ImageIcon.Visibility = Visibility.Visible; // Hiển thị biểu tượng nếu không tải được hình ảnh
                }
            }
            else
            {
                // Nếu không có URL hình ảnh, chỉ cần ẩn LoadingText và hiển thị biểu tượng
                LoadingText.Visibility = Visibility.Collapsed;
                ImageIcon.Visibility = Visibility.Visible;
            }
        }
    }

    // Xử lý sự kiện khi nhấn nút xóa sản phẩm
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Product productToDelete)
        {
            // Hiển thị xác nhận trước khi xóa
            var result = MessageBox.Show($"Are you sure you want to delete product '{productToDelete.product_name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ShowLoading(true); // Hiển thị loading
                try
                {
                    string url =
                        $"/product/delete?product_id={productToDelete.product_id}"; // Replace with your actual API endpoint
                    Console.WriteLine("url: " + url);
                    var parameters = new Dictionary<string, string>
                    {
                        { "product_id", productToDelete.product_id.ToString() }
                    };

                    var apiService = new Api();
                    await apiService.PostApiAsync(url, parameters, (responseBody) =>
                    {
                        try
                        {
                            var responseData = JsonConvert.DeserializeObject<BranchApiResponse<string>>(responseBody);
                            if (responseData != null && responseData.status == 200)
                            {
                                Products.Remove(productToDelete);
                                FilteredProducts.Refresh();
                            }
                            else
                            {
                                MessageBox.Show(
                                    $"Failed to delete product: {responseData?.message ?? "Unknown error"}");
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
        }
    }

    private async void OnImageBorderClick(object sender, MouseButtonEventArgs e)
    {
        // Mở hộp thoại chọn file
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";

        // Nếu người dùng chọn một file và nhấn OK
        if (openFileDialog.ShowDialog() == true)
        {
            isNewImageSelected = true;
            LoadingText.Visibility = Visibility.Visible;
            ImageIcon.Visibility = Visibility.Collapsed;
            SelectedImage.Visibility = Visibility.Collapsed;

            string selectedFilePath = openFileDialog.FileName;

            await Task.Run(() =>
            {
                // Tạo đối tượng BitmapImage để hiển thị hình ảnh
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(selectedFilePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Đảm bảo ảnh có thể được truy cập từ các luồng khác

                // Cập nhật UI trên luồng chính
                Dispatcher.Invoke(() =>
                {
                    SelectedImage.Source = bitmap;
                    LoadingText.Visibility = Visibility.Collapsed;
                    SelectedImage.Visibility = Visibility.Visible;
                });
            });

            // Cập nhật URL hình ảnh trong _selectedProduct với đường dẫn của hình ảnh mới
            if (_selectedProduct != null)
            {
                _selectedProduct.url_image = selectedFilePath;
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Xóa toàn bộ dữ liệu trong các trường input
        Name.Text = string.Empty;
        Amout.Text = string.Empty;

        // Xóa ảnh đang hiển thị và đưa giao diện về trạng thái ban đầu
        SelectedImage.Source = null;
        SelectedImage.Visibility = Visibility.Collapsed;
        ImageIcon.Visibility = Visibility.Visible;

        // Reset trạng thái về thêm sản phẩm mới
        _selectedProduct = null;
        SubmitButton.Content = "Add product";
        isNewImageSelected = false;
    }


    private void ShowLoading(bool show)
    {
        Dispatcher.Invoke(() => { LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed; });
    }
}