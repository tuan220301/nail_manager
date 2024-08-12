using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        Products = new ObservableCollection<Product>
        {
            new Product { ProductId = "Product-123", Name = "Sample product 1", Price = "$20.5", IsChecked = false },
            new Product { ProductId = "Product-124", Name = "Sample product 2", Price = "$25.0", IsChecked = false },
            // Thêm các sản phẩm khác tại đây
        };
        FilteredProducts = CollectionViewSource.GetDefaultView(Products);
        DataContext = this;
        GetBrandList();
    }
    private async void GetBrandList()
    {
        string url = "/branch/list"; // Replace with your API endpoint
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        var apiService = new Api(); // Create an instance of the class containing GetApiAsync

        await apiService.GetApiAsync(url, parameters, (responseBody) =>
        {
            Console.WriteLine("Raw API response:");
            Console.WriteLine(responseBody);

            // Continue with JSON deserialization
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
        });
    }


   private async void OnSubmit(object sender, RoutedEventArgs e)
{
    // Show the loading indicator
    StartLoadingProcess();

    try
    {
        // Retrieve input values
        string name = Name.Text;
        string amount = Amout.Text;
        int branchId = (int)BranchComboBox.SelectedValue;

        // Log values for debugging purposes
        Console.WriteLine("Name: " + name);
        Console.WriteLine("Amount: " + amount);
        Console.WriteLine("Branch ID: " + branchId);

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
                    // Perform further actions on successful response
                    MessageBox.Show("Product successfully added to the branch.");
                }
                else
                {
                    // Handle the case where the API returned an error status
                    MessageBox.Show($"API Error: {responseData?.message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during response processing
                MessageBox.Show($"Error processing API response: {ex.Message}");
            }
        });
    }
    finally
    {
        // Hide the loading indicator regardless of success or failure
        StartLoadingProcess(false);
    }
}

private void StartLoadingProcess(bool isLoading = true)
{
    // Get the MainLayout instance from the parent window
    var mainWindow = Window.GetWindow(this) as MainWindow;
    MainLayout mainLayout = null;

    if (mainWindow != null)
    {
        mainLayout = mainWindow.Content as MainLayout;
        if (mainLayout != null)
        {
            mainLayout.ShowLoading(isLoading);
        }
    }
}


    

    
    private void OnSearch(object sender, RoutedEventArgs e)
    {
        string searchText = ((Grid)((Button)sender).Parent).Children.OfType<TextBox>().FirstOrDefault()?.Text;
        if (!string.IsNullOrEmpty(searchText))
        {
            FilteredProducts.Filter = product =>
            {
                var p = product as Product;
                return p != null && (p.ProductId.Contains(searchText) || p.Name.Contains(searchText) || p.Price.Contains(searchText));
            };
        }
        else
        {
            FilteredProducts.Filter = null;
        }
        FilteredProducts.Refresh();
    }
}
