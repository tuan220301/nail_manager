using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NailManager.Models;
using NailManager.Services;
using Newtonsoft.Json;
using SharpVectors.Renderers.Wpf;
using SharpVectors.Dom.Svg;
using System.Linq;
using System.Net.Http;
using System.Printing;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Windows.Xps;
using SharpVectors.Renderers.Utils;
using Menu = NailManager.Models.Menu;

namespace NailManager.Screen;

public partial class TabBillCreate : UserControl
{
    public ICollectionView FilteredProducts { get; set; }
    public ObservableCollection<Product> Products { get; set; }
    public ObservableCollection<ListService> SelectedItems { get; set; }
    public ObservableCollection<UserFromListApi> Users { get; set; }
    private WpfSvgWindow _wpfWindow;

    public int BranchId { get; set; }

    // public Branch CurrentBranch { get; set; }
    private bool _isInitialized = false;
    private Branch currentBranch;
    private string bill_id_string;

    public TabBillCreate()
    {
        InitializeComponent();
        Products = new ObservableCollection<Product>();
        FilteredProducts = CollectionViewSource.GetDefaultView(Products);
        SelectedItems = new ObservableCollection<ListService>();
        Users = new ObservableCollection<UserFromListApi>();

        var wpfSettings = new WpfDrawingSettings();
        var wpfRenderer = new WpfDrawingRenderer(wpfSettings);
        _wpfWindow = new WpfSvgWindow(500, 500, wpfRenderer);

        DataContext = this;
        GetUser();
        // GetBrandList();
    }

    private async void GetUser()
    {
        ShowLoading(true);
        var user = await DatabaseHelper.GetUserAsync();
        BranchId = user.BranchId;
        GetListProduct(user.BranchId); // Gọi API lần đầu tiên với BranchId đã thiết lập
        GetStaffList(user.BranchId); // Lấy danh sách nhân viên
        GetBrandList(user.BranchId);
        ShowLoading(false);
    }

    private async void GetBrandList(int user_branch_id)
    {
        ShowLoading(true); // Hiển thị loading
        string url = "/branch/list";
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        var apiService = new Api();

        await apiService.GetApiAsync(url, parameters, (responseBody) =>
        {
            try
            {
                var responseData = JsonConvert.DeserializeObject<BranchApiResponse<List<Branch>>>(responseBody);
                // Console.WriteLine("responseData: " + Utls.FormatJsonString(responseBody));
                if (responseData != null && responseData.status == 200)
                {
                    var listBranch = responseData.data;
                    var filterBranch = listBranch.Find(branch => branch.branch_id == user_branch_id);
                    currentBranch = filterBranch;
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

    private async void GetStaffList(int branchId)
    {
        ShowLoading(true);

        string url = $"/user/list?branch_id={branchId}";
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        var apiService = new Api();

        await apiService.GetApiAsync(url, parameters, (responseBody) =>
        {
            try
            {
                if (string.IsNullOrEmpty(responseBody))
                {
                    throw new Exception("API response is empty or null.");
                }

                var responseData =
                    JsonConvert.DeserializeObject<BranchApiResponse<List<UserFromListApi>>>(responseBody);
                // Console.WriteLine("Parsed response object: " + Utls.FormatJsonString(responseBody));

                if (responseData == null || responseData.data == null)
                {
                    throw new Exception("Failed to parse API response.");
                }

                if (responseData.status != 200)
                {
                    throw new Exception("Failed to load users.");
                }

                Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    StaffComboBox.ItemsSource = null; // Xóa các item cũ trong ComboBox

                    foreach (var user in responseData.data)
                    {
                        Users.Add(user);
                    }

                    StaffComboBox.ItemsSource = Users;
                    StaffComboBox.DisplayMemberPath = "name";
                    StaffComboBox.SelectedValuePath = "user_id";

                    // Đặt giá trị mặc định cho ComboBox
                    if (StaffComboBox.Items.Count > 0)
                    {
                        StaffComboBox.SelectedIndex = 0;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                ShowLoading(false); // Ẩn loading khi hoàn tất
            }
        });
    }

    private async void GetListProduct(int branchId)
    {
        Console.WriteLine("branch_id: " + branchId);
        ShowLoading(true);

        string url = $"/product/list?branch_id={branchId}";
        // Console.WriteLine("url: " + url);
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        var apiService = new Api();

        await apiService.GetApiAsync(url, parameters, (responseBody) =>
        {
            try
            {
                if (string.IsNullOrEmpty(responseBody))
                {
                    throw new Exception("API response is empty or null.");
                }

                var responseData = JsonConvert.DeserializeObject<BranchApiResponse<List<Product>>>(responseBody);
                // string responseDataJson = JsonConvert.SerializeObject(responseBody, Formatting.Indented);
                // Console.WriteLine("responseData (JSON) in get product from tab list: ");
                // Console.WriteLine(responseDataJson);
                if (responseData == null)
                {
                    throw new Exception("Failed to parse API response.");
                }

                if (responseData.status != 200)
                {
                    // Console.WriteLine("responseData.status: " + responseData.status);
                    // Console.WriteLine("data error ");
                    Console.WriteLine(responseData.data);
                    throw new Exception("Failed to load products.");
                }


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
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                ShowLoading(false); // Ẩn loading khi hoàn tất
            }
        });
    }

    // private void BranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    // {
    //     if (_isInitialized && BranchComboBox.SelectedValue != null)
    //     {
    //         int selectedBranchId = (int)BranchComboBox.SelectedValue;
    //         currentBranch = BranchComboBox.SelectedItem as Branch;
    //
    //         GetListProduct(selectedBranchId); // Gọi lại với branch_id mới
    //         GetStaffList(selectedBranchId); // Lấy danh sách nhân viên của chi nhánh đã chọn
    //     }
    // }

    // private void OnAddItemClick(object sender, RoutedEventArgs e)
    // {
    //     var button = sender as Button;
    //     var selectedItem = button?.DataContext as Product;
    //
    //     if (selectedItem != null)
    //     {
    //         // Kiểm tra xem sản phẩm đã tồn tại trong danh sách SelectedItems chưa
    //         var existingItem = SelectedItems.FirstOrDefault(item => item.product_id == selectedItem.product_id);
    //
    //         if (existingItem == null) // Nếu sản phẩm chưa có trong danh sách
    //         {
    //             // Truncate product_name nếu vượt quá 15 ký tự
    //             string truncatedName = TruncateString(selectedItem.product_name, 25);
    //             selectedItem.product_name = truncatedName;
    //
    //             selectedItem.Quantity = 1; // Số lượng mặc định là 1
    //             SelectedItems.Add(selectedItem); // Thêm sản phẩm vào danh sách
    //         }
    //         // Nếu sản phẩm đã tồn tại, không thêm và không tăng số lượng
    //
    //         TotalPriceChanged();
    //         RefreshSelectedItems();
    //         UpdateProductSelectionState();
    //     }
    // }

    // private void UpdateProductSelectionState()
    // {
    //     foreach (var product in Products)
    //     {
    //         // Kiểm tra nếu sản phẩm có trong danh sách SelectedItems
    //         product.IsChecked = SelectedItems.Any(item => item.product_id == product.product_id);
    //     }
    // }

    private string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }

    private void OnRemoveItemClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var selectedItem = button?.DataContext as ListService;
        if (selectedItem != null)
        {
            var existingItem = SelectedItems.FirstOrDefault(item => item.user_id == selectedItem.user_id);
            if (existingItem != null)
            {
                SelectedItems.Remove(existingItem);
            }

            TotalPriceChanged();
            RefreshSelectedItems();
            // UpdateProductSelectionState();
        }
    }

    private void RefreshSelectedItems()
    {
        ItemsControl itemsControl = FindName("SelectedItemsControl") as ItemsControl;
        if (itemsControl != null)
        {
            itemsControl.Items.Refresh();
        }
    }

    private void TotalPriceChanged()
    {
        double total = 0.0;
        foreach (var item in SelectedItems)
        {
            total += (double)item.other_price + item.service_fee;
        }

        TotalPrice.Text = total.ToString("F2") + " $"; // Định dạng số thành chuỗi có 2 chữ số thập phân
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

    private FlowDocument CreateBillDocument(string billID)
    {
        string customerName = CustomerNameTextBox?.Text ?? string.Empty;
        string phoneNumber = PhoneNumberTextBox?.Text ?? string.Empty;
        var selectedUser = StaffComboBox?.SelectedItem as UserFromListApi;
        string staff = selectedUser?.user_name ?? "Unknown";
        var selectedItems = SelectedItems ?? new ObservableCollection<ListService>(); // Sử dụng trực tiếp SelectedItems
        string totalPriceText = TotalPrice?.Text.Replace("$", "").Trim() ?? "0";
        double totalPrice = string.IsNullOrWhiteSpace(totalPriceText) ? 0 : double.Parse(totalPriceText);

        FlowDocument document = new FlowDocument
        {
            PageWidth = 275,
            PagePadding = new Thickness(10),
            ColumnWidth = double.PositiveInfinity
        };

        // Add Image (SVG)
        var drawing = RenderSvg("Images/Svgs/logo.svg");
        if (drawing != null)
        {
            var drawingImage = new DrawingImage(drawing);
            Image svgImage = new Image
            {
                Source = drawingImage,
                Width = 100,
                Height = 50,
                Stretch = Stretch.Uniform
            };

            BlockUIContainer imageContainer = new BlockUIContainer(svgImage);
            document.Blocks.Add(imageContainer);
        }


        document.Blocks.Add(new Paragraph(new Run(currentBranch.name))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Roboto"),
        });
        document.Blocks.Add(new Paragraph(new Run(currentBranch.description))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Roboto"),
        });
        document.Blocks.Add(new Paragraph(new Run(currentBranch.website))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Roboto"),
        });

        // Title
        document.Blocks.Add(new Paragraph(new Run("Bill"))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Roboto"),
        });

        document.Blocks.Add(new Paragraph(new Run($"Customer: {customerName}\nPhone number: {phoneNumber}"))
        {
            FontSize = 12,
            TextAlignment = TextAlignment.Left,
            FontFamily = new FontFamily("Roboto"),
        });

        // Bill Info
        string billIdString = billID ?? "Unknown"; // Ensure bill_id_string is not null
        document.Blocks.Add(
            new Paragraph(new Run(
                $"Bill ID: #{billIdString}\nStaff: {staff}\nFrom: {DateTime.Now:dd/MM/yyyy} {DateTime.Now:HH:mm}"))
            {
                FontSize = 12,
                TextAlignment = TextAlignment.Left,
                FontFamily = new FontFamily("Roboto"),
            });

        // Table Header
        document.Blocks.Add(CreateHeaderRow(new[] { "Employee", "Price", "Fee", "Total" }));

        // Table Rows for Selected Items
        foreach (var item in selectedItems)
        {
            document.Blocks.Add(CreateLineRow(new[]
            {
                item.user_name,
                item.other_price.ToString("N2"),
                item.service_fee.ToString("N2"),
                (item.other_price + item.service_fee).ToString("N2"),
            }));
        }

        // Total
        document.Blocks.Add(new Paragraph(new Run($"\nTotal: {totalPrice} $"))
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Right,
            FontFamily = new FontFamily("Roboto"),
        });


        document.Blocks.Add(new Paragraph(new Run(currentBranch.address))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Roboto"),
        });
        document.Blocks.Add(new Paragraph(new Run(currentBranch.sdt))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Roboto"),
        });
        document.Blocks.Add(new Paragraph(new Run(currentBranch.open_close))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Roboto"),
        });

        return document;
    }


    private BlockUIContainer CreateHeaderRow(string[] columns)
    {
        Border rowBorder = new Border
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1, 1, 1, 1),
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };

        Grid grid = new Grid();

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

        for (int i = 0; i < columns.Length; i++)
        {
            TextBlock textBlock = new TextBlock
            {
                Text = columns[i],
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(1),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                FontFamily = new FontFamily("Roboto")
            };

            Border cellBorder = new Border
            {
                BorderBrush = Brushes.Black,
                Width = (double)grid.ColumnDefinitions[i].Width.Value,
                BorderThickness = new Thickness(
                    left: (i == 1 || i == 2 || i == 3 || i == 4) ? 1 : 0,
                    top: 0,
                    right: 0,
                    bottom: 0),
                Child = textBlock
            };

            Grid.SetColumn(cellBorder, i);
            grid.Children.Add(cellBorder);
        }

        rowBorder.Child = grid;
        return new BlockUIContainer(rowBorder);
    }

    private BlockUIContainer CreateLineRow(string[] columns)
    {
        Border rowBorder = new Border
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1, 0, 1, 1),
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };

        Grid grid = new Grid();

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

        for (int i = 0; i < columns.Length; i++)
        {
            TextBlock textBlock = new TextBlock
            {
                Text = columns[i],
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(1),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                FontFamily = new FontFamily("Roboto")
            };

            Border cellBorder = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(
                    left: (i == 1 || i == 2 || i == 3 || i == 4) ? 1 : 0,
                    top: 0,
                    right: 0,
                    bottom: 0),
                Child = textBlock
            };

            Grid.SetColumn(cellBorder, i);
            grid.Children.Add(cellBorder);
        }

        rowBorder.Child = grid;
        return new BlockUIContainer(rowBorder);
    }

    private void PreviewBill(FlowDocument document)
    {
        Window previewWindow = new Window
        {
            Title = "Bill Preview",
            Width = 300,
            Height = 400,
            Content = new FlowDocumentScrollViewer
            {
                Document = document,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible
            }
        };
        previewWindow.ShowDialog();
    }

    private async void CreateBill_Click(object sender, RoutedEventArgs e)
    {
        // Kiểm tra đầu vào xem có đầy đủ thông tin chưa
        if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(PhoneNumberTextBox.Text) ||
            StaffComboBox.SelectedItem == null ||
            !SelectedItems.Any())
        {
            MessageBox.Show("Please fill full information.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return; // Dừng quá trình nếu thông tin không đầy đủ
        }

        ShowLoading(true); // Hiển thị loading
        try
        {
            // Lấy thông tin từ form
            string customerName = CustomerNameTextBox.Text;
            string customerPhone = PhoneNumberTextBox.Text;

            // Lấy thông tin nhân viên từ ComboBox
            // var selectedUser = StaffComboBox.SelectedItem as UserFromListApi;
            // int userId = selectedUser?.user_id ?? 0;

            // Lấy giá trị từ OrderServiceTextBox, nếu rỗng thì mặc định là 0
            string otherPriceText = OrderServiceTextBox.Text;

            // Danh sách sản phẩm được chọn
            var service = SelectedItems;

            // Chuẩn bị parameters dưới dạng JSON string
            var parameters = new Dictionary<string, object>
            {
                { "customer_name", customerName },
                { "customer_phone", customerPhone },
                { "branch_id", BranchId.ToString() },
                { "service", service },
            };

            string jsonParameters = JsonConvert.SerializeObject(parameters); // Chuyển đổi sang chuỗi JSON

            // Đường dẫn API
            var apiService = new Api();
            string apiUrl = "/bill/create"; // Thay bằng endpoint thực tế của bạn

            // Gọi PostApiAsync để gửi yêu cầu
            await apiService.PostApiAsync(apiUrl, jsonParameters, (responseBody) =>
            {
                try
                {
                    var responseData = JsonConvert.DeserializeObject<BranchApiResponse<BillResponseData>>(responseBody);

                    if (responseData != null && responseData.status == 200)
                    {
                        Console.WriteLine("Create success");
                        Console.WriteLine(responseData.data.bill_id.ToString());
                        string billId = responseData.data.bill_id.ToString();
                        // Nếu lưu thành công, thực hiện in hóa đơn
                        FlowDocument document = CreateBillDocument(billId);
                        PrintBill(document);
                        ClearInputs();
                    }
                    else
                    {
                        Console.WriteLine($"API Error: {responseData?.message ?? "Unknown error"}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing response: {ex.Message}");
                    MessageBox.Show($"Error parsing response: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}");
        }
        finally
        {
            ShowLoading(false); // Ẩn loading khi hoàn tất
        }
    }


    private void PrintBill(FlowDocument document)
    {
        PrintDialog printDialog = new PrintDialog();

        // Lấy máy in mặc định và in tài liệu mà không cần hiển thị hộp thoại in
        var printQueue = printDialog.PrintQueue;
        XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(printQueue);
        writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);
    }

    private void ShowLoading(bool show)
    {
        Dispatcher.Invoke(() => { LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed; });
    }

    private void ClearInputs()
    {
        // Xóa dữ liệu trong các TextBox
        CustomerNameTextBox.Text = string.Empty;
        PhoneNumberTextBox.Text = string.Empty;
        TotalPrice.Text = "0.00 $"; // Đặt lại giá trị tổng tiền

        // Xóa danh sách các sản phẩm đã chọn
        SelectedItems.Clear();

        // Làm mới ItemsControl để cập nhật giao diện
        RefreshSelectedItems();
    }

    private void OnSubmit(object sender, RoutedEventArgs e)
    {
        // Lấy giá trị từ TextBox (giá tiền cho dịch vụ)
        string inputPriceText = OrderServiceTextBox.Text;
        var selectedUser = StaffComboBox.SelectedItem as UserFromListApi;
        string inputServiceFeeText = ServiceFee.Text;
        // Kiểm tra xem người dùng có nhập giá hợp lệ không
        if (double.TryParse(inputPriceText, out double servicePrice))
        {
            // Kiểm tra xem sản phẩm "Other service" đã tồn tại trong danh sách SelectedItems hay chưa
            var existingService = SelectedItems.FirstOrDefault(item => item.user_id == selectedUser.user_id);

            if (existingService != null)
            {
                // Nếu sản phẩm đã tồn tại, cập nhật giá mới nhất
                existingService.other_price = double.Parse(inputPriceText);
                existingService.service_fee = double.Parse(inputServiceFeeText);
            }
            else
            {
                // Nếu chưa tồn tại, tạo sản phẩm mới với tên "Other service"
                // var newProduct = new Product
                // {
                //     product_name = selectedUser.user_name, // Tên sản phẩm
                //     price = servicePrice, // Giá tiền nhập vào
                //     fee = double.Parse(inputServiceFeeText)
                // };
                var newService = new ListService
                {
                    product = [],
                    user_id = selectedUser.user_id,
                    user_name = selectedUser.name,
                    other_price = servicePrice,
                    service_fee = double.Parse(inputServiceFeeText)
                };

                // Thêm sản phẩm mới vào danh sách SelectedItems
                SelectedItems.Add(newService);
            }

            // Cập nhật tổng tiền sau khi thêm/cập nhật sản phẩm
            TotalPriceChanged();

            // Làm mới danh sách SelectedItems trên giao diện người dùng
            RefreshSelectedItems();

            // Cập nhật trạng thái chọn sản phẩm
            // UpdateProductSelectionState();
        }
        else
        {
            // Nếu giá trị không hợp lệ, thông báo lỗi
            MessageBox.Show("Please enter a valid price for Other service.", "Invalid Input", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}