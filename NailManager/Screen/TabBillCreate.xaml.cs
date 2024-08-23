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
    public ObservableCollection<Product> SelectedItems { get; set; }
    public ObservableCollection<UserFromListApi> Users { get; set; }
    private WpfSvgWindow _wpfWindow;
    public int BranchId { get; set; }
    private bool _isInitialized = false;

    public TabBillCreate()
    {
        InitializeComponent();
        Products = new ObservableCollection<Product>();
        FilteredProducts = CollectionViewSource.GetDefaultView(Products);
        SelectedItems = new ObservableCollection<Product>();
        Users = new ObservableCollection<UserFromListApi>();

        var wpfSettings = new WpfDrawingSettings();
        var wpfRenderer = new WpfDrawingRenderer(wpfSettings);
        _wpfWindow = new WpfSvgWindow(500, 500, wpfRenderer);

        DataContext = this;
        GetUser();
        GetBrandList();
    }

    private async void GetUser()
    {
        ShowLoading(true);
        var user = await DatabaseHelper.GetUserAsync();
        if (user != null)
        {
            Console.WriteLine("user.BranchId in create: " + user.BranchId);
            BranchId = user.BranchId > 0 ? user.BranchId : 1; // Đặt BranchId là 1 nếu admin
            FilterBranch.Visibility = user.BranchId > 0 ? Visibility.Collapsed : Visibility.Visible;

            // Thiết lập giá trị mặc định cho ComboBox, điều này sẽ kích hoạt SelectionChanged nếu không ngăn chặn
            BranchComboBox.SelectedValue = BranchId;

            _isInitialized = true; // Đánh dấu là đã khởi tạo xong
            GetListProduct(BranchId); // Gọi API lần đầu tiên với BranchId đã thiết lập
            GetStaffList(BranchId); // Lấy danh sách nhân viên

            ShowLoading(false);
        }
    }

    private async void GetBrandList()
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
                    StaffComboBox.DisplayMemberPath = "user_name";
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

    private async void GetListProduct(int branchId = 1)
    {
        ShowLoading(true);

        string url = $"/product/list?branch_id={branchId}";
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

                if (responseData == null)
                {
                    throw new Exception("Failed to parse API response.");
                }

                if (responseData.status != 200 || responseData.data == null)
                {
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

    private void BranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitialized && BranchComboBox.SelectedValue != null)
        {
            int selectedBranchId = (int)BranchComboBox.SelectedValue;
            GetListProduct(selectedBranchId); // Gọi lại với branch_id mới
            GetStaffList(selectedBranchId); // Lấy danh sách nhân viên của chi nhánh đã chọn
        }
    }

    private void OnAddItemClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var selectedItem = button?.DataContext as Product;

        if (selectedItem != null)
        {
            // Kiểm tra xem sản phẩm đã tồn tại trong danh sách SelectedItems chưa
            var existingItem = SelectedItems.FirstOrDefault(item => item.product_id == selectedItem.product_id);

            if (existingItem == null) // Nếu sản phẩm chưa có trong danh sách
            {
                selectedItem.Quantity = 1; // Số lượng mặc định là 1
                SelectedItems.Add(selectedItem); // Thêm sản phẩm vào danh sách
            }
            // Nếu sản phẩm đã tồn tại, không thêm và không tăng số lượng

            TotalPriceChanged();
            RefreshSelectedItems();
        }
    }


    private void OnRemoveItemClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var selectedItem = button?.DataContext as Product;
        if (selectedItem != null)
        {
            var existingItem = SelectedItems.FirstOrDefault(item => item.product_name == selectedItem.product_name);
            if (existingItem != null)
            {
                existingItem.Quantity--;
                if (existingItem.Quantity == 0)
                {
                    SelectedItems.Remove(existingItem);
                }
            }

            TotalPriceChanged();
            RefreshSelectedItems();
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
            total += (double)item.price * item.Quantity;
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

    private FlowDocument CreateBillDocument()
    {
        string customerName = CustomerNameTextBox.Text;
        string phoneNumber = PhoneNumberTextBox.Text;
        var selectedUser = StaffComboBox.SelectedItem as UserFromListApi;
        string staff = selectedUser?.user_name;
        var selectedItems = SelectedItemsControl.ItemsSource as ObservableCollection<Product>;
        string totalPriceText = TotalPrice.Text.Replace("$", "").Trim();
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

        // Title
        Paragraph title = new Paragraph(new Run("Bill Invoice (PROCESSING)"))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Roboto"),
        };
        document.Blocks.Add(title);

        // Bill Info
        Paragraph billInfo =
            new Paragraph(new Run(
                $"InvoiceID: #12345\nStaff: {staff}\nFrom: {DateTime.Now:dd/MM/yyyy} {DateTime.Now:HH:mm}"))
            {
                FontSize = 12,
                TextAlignment = TextAlignment.Left,
                FontFamily = new FontFamily("Roboto"),
            };
        document.Blocks.Add(billInfo);

        // Table Header
        document.Blocks.Add(CreateHeaderRow(new[] { "NO", "Name", "Price", "Total" }));

        // Table Rows
        int index = 1;
        foreach (var item in selectedItems)
        {
            double price = (double)item.price;
            double total = price * item.Quantity;
            string formattedPrice = price % 1 == 0 ? price.ToString("N0") : price.ToString("N2");
            string formattedTotal = total % 1 == 0 ? total.ToString("N0") : total.ToString("N2");

            document.Blocks.Add(CreateLineRow(new[]
            {
                index.ToString(),
                item.product_name,
                formattedPrice,
                formattedTotal
            }));
            index++;
        }

        // Total
        Paragraph totalParagraph = new Paragraph(new Run($"\nTotal: {totalPrice:N0} $"))
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Right,
            FontFamily = new FontFamily("Roboto"),
        };
        document.Blocks.Add(totalParagraph);

        // Store Info
        // Paragraph storeInfo =
        //     new Paragraph(new Run(
        //         "CA ZONE - BÌNH THẠNH\nAddress: 32/6 Hẻm 36 Nguyễn Gia Trí, P25, Quận Bình Thạnh, TP.HCM\nHotline: 0325483193"))
        //     {
        //         FontSize = 10,
        //         TextAlignment = TextAlignment.Center,
        //         FontFamily = new FontFamily("Roboto"),
        //     };
        // document.Blocks.Add(storeInfo);

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

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });

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

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });

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
        PaymentMethod.SelectedItem == null || // Kiểm tra giá trị của ComboBox PaymentMethod
        !SelectedItems.Any())
    {
        MessageBox.Show("Vui lòng điền đầy đủ thông tin và chọn ít nhất một sản phẩm.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
        return; // Dừng quá trình nếu thông tin không đầy đủ
    }

    ShowLoading(true); // Hiển thị loading
    try
    {
        // Lấy thông tin từ form
        string customerName = CustomerNameTextBox.Text;
        string customerPhone = CustomerNameTextBox.Text;

        // Lấy thông tin nhân viên từ ComboBox
        var selectedUser = StaffComboBox.SelectedItem as UserFromListApi;
        int userId = selectedUser?.user_id ?? 0;
        string branchId = BranchComboBox.SelectedValue.ToString();

        // Lấy giá trị phương thức thanh toán từ ComboBox, xử lý nếu null
        int paymentMethod = PaymentMethod.SelectedItem != null 
                            ? int.Parse(((ComboBoxItem)PaymentMethod.SelectedItem).Tag.ToString()) 
                            : 0; // Hoặc giá trị mặc định, cần xử lý theo logic của bạn

        // Kiểm tra lại nếu paymentMethod là 0
        if (paymentMethod == 0)
        {
            MessageBox.Show("Vui lòng chọn phương thức thanh toán.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Danh sách sản phẩm được chọn
        var productIds = SelectedItems.Select(item => item.product_id).ToList();

        // Tạo object chứa các tham số theo đúng định dạng JSON yêu cầu
        var parameters = new
        {
            customer_name = customerName,
            customer_phone = customerPhone,
            branch_id = branchId,
            user_id = userId,
            discount = 0,
            pay_method = paymentMethod,
            products = productIds
        };

        // Chuyển đổi object parameters thành chuỗi JSON
        string jsonContent = JsonConvert.SerializeObject(parameters);

        // In chuỗi JSON ra để kiểm tra
        Console.WriteLine("JSON Content:");
        Console.WriteLine(jsonContent);

        // Đường dẫn API
        var api = new ApiConnect();
        string url = api.Url + "/bill/create"; // Thay bằng endpoint thực tế của bạn
        Console.WriteLine("url: " + url);
        
        // Thực hiện gọi API với dữ liệu JSON
        using (var httpClient = new HttpClient())
        {
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<BranchApiResponse<BillResponseData>>(responseBody);

                if (responseData != null && responseData.status == 200)
                {
                    Console.WriteLine("Create success");
                    Console.WriteLine(responseData.data);
                    // Nếu lưu thành công, thực hiện in hóa đơn
                    FlowDocument document = CreateBillDocument();
                    PrintBill(document);
                    ClearInputs();
                }
                else
                {
                    Console.WriteLine($"API Error: {responseData?.message ?? "Unknown error"}");
                }
            }
            else
            {
                Console.WriteLine("Failed to create the bill.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
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
}