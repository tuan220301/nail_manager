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
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Windows.Input;
using SharpVectors.Renderers.Utils;
using Menu = NailManager.Models.Menu;

namespace NailManager.Screen
{
    public partial class TabBillList : UserControl, INotifyPropertyChanged
    {
        private bool _isBillSelected;

        public bool IsBillSelected
        {
            get => _isBillSelected;
            set
            {
                _isBillSelected = value;
                OnPropertyChanged(nameof(IsBillSelected));
            }
        }

        public ObservableCollection<BillFromList> Bill { get; set; }
        public ICollectionView FilteredBill { get; set; }
        public ObservableCollection<UserFromListApi> Users { get; set; }
        public ICollectionView FilteredUsers { get; set; }
        public ObservableCollection<ProductInBill> SelectedItems { get; set; }
        public ObservableCollection<Branch> Branches { get; set; }
        private WpfSvgWindow _wpfWindow;
        private ApiConnect _apiConnect = new ApiConnect();
        public ObservableCollection<Product> Products { get; set; }
        public int BranchId { get; set; }
        public string Address { get; set; }
        public ICollectionView FilteredProducts { get; set; }
        private bool _isInitialized = false;

        private BillFromList _selectedBill;
        private bool _isProductSelected = false;

        public BillFromList SelectedBill
        {
            get => _selectedBill;
            set
            {
                _selectedBill = value;
                OnPropertyChanged(nameof(SelectedBill));
            }
        }

        public bool IsProductSelected
        {
            get => _isProductSelected;
            set
            {
                _isProductSelected = value;
                OnPropertyChanged(nameof(IsProductSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TabBillList()
        {
            InitializeComponent();
            Bill = new ObservableCollection<BillFromList>();
            Products = new ObservableCollection<Product>(); // Khởi tạo Products
            FilteredBill = CollectionViewSource.GetDefaultView(Bill);
            FilteredProducts = CollectionViewSource.GetDefaultView(Products);
            SelectedItems = new ObservableCollection<ProductInBill>();
            Users = new ObservableCollection<UserFromListApi>(); // Khởi tạo ObservableCollection cho Users
            Branches = new ObservableCollection<Branch>(); // Khởi tạo ObservableCollection cho Branches
            var wpfSettings = new WpfDrawingSettings();
            var wpfRenderer = new WpfDrawingRenderer(wpfSettings);
            _wpfWindow = new WpfSvgWindow(500, 500, wpfRenderer);
            DataContext = this;
            IsBillSelected = false;
            GetBrandList();
            GetUser();
        }

        private async void GetBrandList()
        {
            ShowLoading(true); // Hiển thị loading
            string url = "/branch/list";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            var apiService = new Api();

            try
            {
                await apiService.GetApiAsync(url, parameters, (responseBody) =>
                {
                    var responseData = JsonConvert.DeserializeObject<BranchApiResponse<List<Branch>>>(responseBody);

                    if (responseData != null && responseData.status == 200)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            BranchComboBox.ItemsSource = responseData.data;
                            BranchComboBox.DisplayMemberPath = "name";
                            BranchComboBox.SelectedValuePath = "branch_id";
                            BranchComboBox.SelectedValue =
                                BranchId > 0 ? BranchId : 1; // Đặt giá trị mặc định là branch_id = 1
                        });
                    }
                    else
                    {
                        MessageBox.Show("Failed to load branches.");
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading branches: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async void OnBillItemClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is BillFromList selectedBill)
            {
                SelectedBill = selectedBill;

                // Cập nhật IsBillSelected dựa trên trạng thái của hóa đơn
                IsBillSelected = selectedBill.status == 1; // Chỉ cho phép chọn nếu status là PROCESSING

                IsProductSelected = IsBillSelected; // Chỉ cho phép nhấn nút Plus nếu hóa đơn đang xử lý

                // Gọi API để lấy chi tiết hóa đơn
                string apiUrl = $"{_apiConnect.Url}/bill/detail?bill_id={selectedBill.bill_id}";
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync(apiUrl);
                        response.EnsureSuccessStatusCode();

                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<BranchApiResponse<BillDetail>>(responseBody);

                        if (responseData != null && responseData.status == 200)
                        {
                            var billDetail = responseData.data;
                            CustomerNameTextBox.Text = billDetail.customer_name;
                            PhoneNumberTextBox.Text = billDetail.customer_phone;
                            TotalPrice.Text = $"{billDetail.total_price} $";
                            UserIdTextBox.Text = billDetail.user_id.ToString();
                            StaffTextBox.Text = billDetail.name;
                            BillIdTextBox.Text = billDetail.bill_id.ToString();

                            SelectedItems.Clear();
                            foreach (var product in billDetail.products)
                            {
                                var matchingProduct = Products.FirstOrDefault(p => p.product_id == product.product_id);
                                string productName = matchingProduct != null
                                    ? matchingProduct.product_name
                                    : "Unknown Product";

                                SelectedItems.Add(new ProductInBill()
                                {
                                    product_id = product.product_id,
                                    name = productName,
                                    price = product.price,
                                });
                            }

                            RefreshSelectedItems();
                        }
                        else
                        {
                            MessageBox.Show($"API Error: {responseData?.message ?? "Unknown error"}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fetching bill details: {ex.Message}");
                }
            }
        }


        private void OnPlusButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Product selectedProduct)
            {
                // Kiểm tra xem sản phẩm đã tồn tại trong SelectedItems chưa
                var existingItem = SelectedItems.FirstOrDefault(item => item.product_id == selectedProduct.product_id);

                if (existingItem != null)
                {
                    // Nếu sản phẩm đã tồn tại, không làm gì cả hoặc hiển thị thông báo
                    MessageBox.Show("This product is already in the list.");
                }
                else
                {
                    // Nếu sản phẩm chưa tồn tại, thêm sản phẩm mới vào danh sách
                    SelectedItems.Add(new ProductInBill
                    {
                        product_id = selectedProduct.product_id,
                        name = selectedProduct.product_name,
                        price = selectedProduct.price,
                        IsNewlyAdded = true // Đánh dấu sản phẩm mới được thêm
                    });

                    // Cập nhật tổng tiền
                    TotalPriceChanged();
                    RefreshSelectedItems();
                }
            }
        }


        private void OnRemoveItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductInBill product)
            {
                SelectedItems.Remove(product);
                TotalPriceChanged();
            }
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

        private void RefreshSelectedItems()
        {
            ItemsControl itemsControl = FindName("SelectedItemsControl") as ItemsControl;
            if (itemsControl != null)
            {
                itemsControl.Items.Refresh();
            }
        }

        private async void UpdateBill_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;

            // Nếu nút "Cancel" được nhấn, hiển thị cảnh báo
            if (clickedButton != null && clickedButton.Content.ToString() == "Cancel")
            {
                MessageBoxResult result = MessageBox.Show(
                    "Canceling this bill will make it uneditable. Are you sure you want to cancel?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                // Nếu người dùng chọn "No", dừng quá trình
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            ShowLoading(true); // Hiển thị loading

            try
            {
                // Lấy thông tin từ form
                string customerName = CustomerNameTextBox.Text;
                string customerPhone = PhoneNumberTextBox.Text;

                // Lấy thông tin nhân viên từ TextBox
                int userId = int.Parse(UserIdTextBox.Text);
                string branchId = BranchComboBox.SelectedValue.ToString();

                // Lấy thông tin giảm giá từ TextBox (giả sử bạn có một TextBox để nhập giảm giá)
                int discount = 0;

                // Tạo danh sách sản phẩm với product_id
                var products = SelectedItems.Select(item => new { product_id = item.product_id }).ToList();

                // Tạo object chứa các tham số theo đúng định dạng JSON yêu cầu
                var parameters = new
                {
                    customer_name = customerName,
                    customer_phone = customerPhone,
                    branch_id = branchId,
                    user_id = userId,
                    discount = discount,
                    products = products
                };

                // Chuyển đổi object parameters thành chuỗi JSON
                string jsonContent = JsonConvert.SerializeObject(parameters);

                // Xác định bill_id và status từ nội dung của nút được nhấn
                int status = 0; // Mặc định là Cancel

                if (clickedButton != null && clickedButton.Content.ToString() == "Print")
                {
                    status = 2; // Trạng thái "Print"
                }

                // Thay bằng giá trị thực tế của bill_id từ hệ thống của bạn
                int billId = int.Parse(BillIdTextBox.Text);

                // Đường dẫn API với bill_id và status tương ứng
                var api = new ApiConnect();
                string url = $"{api.Url}/bill/update?bill_id={billId}&status={status}";
                Console.WriteLine("status: " + status);
                // Thực hiện gọi API với dữ liệu JSON
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseData =
                            JsonConvert.DeserializeObject<BranchApiResponse<BillResponseData>>(responseBody);

                        if (responseData != null && responseData.status == 200)
                        {
                            GetBills();
                            if (status == 2)
                            {
                                FlowDocument document = CreateBillDocument();
                                PrintBill(document);
                                ClearOrderDetails();  
                            }
                        }
                        else
                        {
                            Console.WriteLine($"API Error: {responseData?.message ?? "Unknown error"}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to update the bill.");
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


        private void TotalPriceChanged()
        {
            double total = 0.0;
            foreach (var item in SelectedItems)
            {
                total += item.price; // Cộng giá của mỗi sản phẩm
            }

            TotalPrice.Text = total.ToString("F2") + " $"; // Định dạng số thành chuỗi có 2 chữ số thập phân
        }


        private void BranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BranchComboBox.SelectedValue != null)
            {
                int branchId = (int)BranchComboBox.SelectedValue;
                Console.WriteLine("branchId in get BranchComboBox_SelectionChanged: " + branchId);
                DateTime fromDate = DateTime.Today;
                DateTime toDate = DateTime.Today;
                if (BranchComboBox.SelectedItem is Branch selectedBranch)
                {
                    Address = selectedBranch.address; // Lấy địa chỉ từ đối tượng Branch
                    Console.WriteLine("Selected Branch Address: " + Address);
                    // Thực hiện các hành động khác với địa chỉ này, ví dụ hiển thị trên UI hoặc lưu trữ
                }
                GetListProduct(branchId);
                GetBills(fromDate, toDate, branchId);
            }
        }

        private async void GetBills(DateTime? fromDate = null, DateTime? toDate = null, int branchId = 0)
        {
            ShowLoading(true);

            try
            {
                // Sử dụng branchId được truyền vào hoặc mặc định là BranchId
                branchId = branchId > 0 ? branchId : BranchId;
                fromDate ??= DateTime.Today;
                toDate ??= DateTime.Today;

                string apiUrl = $"{_apiConnect.Url}/bill?branch_id={branchId}&status=1";
                string startDay = fromDate.Value.ToString("yyyy-MM-dd 00:00:00");
                string endDay = toDate.Value.ToString("yyyy-MM-dd 23:59:59");
                apiUrl += $"&start_day={startDay}&end_day={endDay}";
                Console.WriteLine("apiUrl: " + apiUrl);
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseData =
                        JsonConvert.DeserializeObject<BranchApiResponse<List<BillFromList>>>(responseBody);
                    // var responseconvert = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    // Console.WriteLine("responseconvert");
                    // Console.WriteLine(responseconvert);
                    if (responseData != null && responseData.status == 200)
                    {
                        Bill.Clear();
                        foreach (var bill in responseData.data)
                        {
                            Bill.Add(bill);
                        }

                        FilteredBill.Refresh();
                    }
                    else
                    {
                        MessageBox.Show($"API Error: {responseData?.message ?? "Unknown error"}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching bills: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
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

        private FlowDocument CreateBillDocument()
        {
            string customerName = CustomerNameTextBox.Text;
            string phoneNumber = PhoneNumberTextBox.Text;
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
            Paragraph title = new Paragraph(new Run("Bill Invoice (COMPLETE)"))
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
                    $"InvoiceID: #12345\nStaff: {StaffTextBox.Text}\nFrom: {DateTime.Now:dd/MM/yyyy} {DateTime.Now:HH:mm}"))
                {
                    FontSize = 12,
                    TextAlignment = TextAlignment.Left,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(billInfo);

            // Table Header
            document.Blocks.Add(CreateHeaderRow(new[] { "NO", "Name", "Price" }));

            // Table Rows
            int index = 1;
            foreach (var item in SelectedItems)
            {
                double price = (double)item.price;
                double total = price * item.Quantity;
                string formattedPrice = price % 1 == 0 ? price.ToString("N0") : price.ToString("N2");
                string formattedTotal = total % 1 == 0 ? total.ToString("N0") : total.ToString("N2");

                document.Blocks.Add(CreateLineRow(new[]
                {
                    index.ToString(),
                    item.name,
                    formattedPrice,
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
            Paragraph storeInfo =
                new Paragraph(new Run(
                    Address))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(storeInfo);

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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
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

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
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

        private void PrintBill(FlowDocument document)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource idpSource = document;
                printDialog.PrintDocument(idpSource.DocumentPaginator, "Bill Print");
            }
        }

        private async void GetUser()
        {
            ShowLoading(true);

            try
            {
                var user = await DatabaseHelper.GetUserAsync();
                if (user != null)
                {
                    Console.WriteLine("user.BranchId in create: " + user.BranchId);
                    BranchId = user.BranchId > 0 ? user.BranchId : 1; // Đặt BranchId là 1 nếu admin
                    // Cập nhật lại FilterBranch.Visibility sau khi thiết lập giá trị của ComboBox
                    Dispatcher.Invoke(() =>
                    {
                        BranchComboBox.SelectedValue = BranchId;
                        FilterBranch.Visibility = user.BranchId > 0 ? Visibility.Collapsed : Visibility.Visible;
                    });

                    _isInitialized = true; // Đánh dấu là đã khởi tạo xong
                    GetListProduct(BranchId); // Gọi API lần đầu tiên với BranchId đã thiết lập
                    GetBills();
                }
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void ClearOrderDetails()
        {
            UserIdTextBox.Text = string.Empty;
            StaffTextBox.Text = string.Empty;
            CustomerNameTextBox.Text = string.Empty;
            PhoneNumberTextBox.Text = string.Empty;
            BillIdTextBox.Text = string.Empty;
            TotalPrice.Text = string.Empty;

            SelectedItems.Clear();
            RefreshSelectedItems();
        }

        private void OnClearOrderDetailsClick(object sender, RoutedEventArgs e)
        {
            // Xóa các giá trị trong các TextBox
            ClearOrderDetails();
            RefreshSelectedItems();
        }


        private void ShowLoading(bool show)
        {
            Dispatcher.Invoke(() => { LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed; });
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}