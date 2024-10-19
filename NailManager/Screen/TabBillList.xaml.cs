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
using System.Windows.Input;
using System.Windows.Xps;
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

        private bool _isAdmin;

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                _isAdmin = value;
                OnPropertyChanged(nameof(IsAdmin));
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

        public string permision = "";
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

                // Xây dựng URL API và tham số
                string apiUrl = "/bill/detail";
                var parameters = new Dictionary<string, string>
                    { };
                string param = JsonConvert.SerializeObject(parameters, Formatting.Indented);
                Console.WriteLine("parameters");
                Console.WriteLine(param);
                var apiService = new Api();
                try
                {
                    Console.WriteLine($"{apiUrl}?bill_id={selectedBill.bill_id.ToString()}");
                    // Gọi phương thức GetApiAsync để thực hiện yêu cầu GET
                    await apiService.GetApiAsync($"{apiUrl}?bill_id={selectedBill.bill_id.ToString()}", parameters,
                        (responseBody) =>
                        {
                            try
                            {
                                var responseData =
                                    JsonConvert.DeserializeObject<BranchApiResponse<BillDetail>>(responseBody);

                                if (responseData != null && responseData.status == 200)
                                {
                                    var billDetail = responseData.data;

                                    // Cập nhật UI với thông tin từ billDetail
                                    CustomerNameTextBox.Text = billDetail.customer_name;
                                    PhoneNumberTextBox.Text = billDetail.customer_phone;
                                    TotalPrice.Text = $"{billDetail.total_price} $";
                                    UserIdTextBox.Text = billDetail.user_id.ToString();
                                    StaffTextBox.Text = billDetail.name;
                                    BillIdTextBox.Text = billDetail.bill_id.ToString();
                                    PaymentMethod.SelectedIndex = billDetail.pay_method == 1 ? 0 : 1;

                                    SelectedItems.Clear();
                                    // string productsJson =
                                    //     JsonConvert.SerializeObject(billDetail.products, Formatting.Indented);
                                    // Console.WriteLine("Bill Detail Products:");
                                    // Console.WriteLine(productsJson);
                                    foreach (var product in billDetail.products)
                                    {
                                        // Kiểm tra độ dài của product_name trước khi cắt
                                        string displayName = product.product_name.Length > 25
                                            ? product.product_name.Substring(0, 25) + "..."
                                            : product.product_name;

                                        SelectedItems.Add(new ProductInBill()
                                        {
                                            product_id = product.product_id,
                                            product_name = displayName,
                                            price = product.price,
                                        });
                                    }

                                    RefreshSelectedItems();
                                    UpdateProductSelectionState(); 
                                }
                                else
                                {
                                    MessageBox.Show($"API Error: {responseData?.message ?? "Unknown error"}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing response: {ex.Message}");
                                MessageBox.Show($"Error parsing response: {ex.Message}", "Error", MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        });
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
                    string displayName = selectedProduct.product_name.Length > 25
                        ? selectedProduct.product_name.Substring(0, 25) + "..."
                        : selectedProduct.product_name;
                    // Nếu sản phẩm chưa tồn tại, thêm sản phẩm mới vào danh sách
                    SelectedItems.Add(new ProductInBill
                    {
                        product_id = selectedProduct.product_id,
                        product_name = displayName,
                        price = selectedProduct.price,
                        IsNewlyAdded = true // Đánh dấu sản phẩm mới được thêm
                    });

                    // Cập nhật tổng tiền
                    TotalPriceChanged();
                    RefreshSelectedItems();
                    UpdateProductSelectionState();
                }
            }
        }


        private void OnRemoveItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductInBill product)
            {
                SelectedItems.Remove(product);
                TotalPriceChanged();
                UpdateProductSelectionState();
            }
        }
        private void UpdateProductSelectionState()
        {
            foreach (var product in Products)
            {
                // Kiểm tra nếu sản phẩm có trong danh sách SelectedItems
                product.IsChecked = SelectedItems.Any(item => item.product_id == product.product_id);
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
                    // Console.WriteLine("GetListProduct: " + responseData);
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
            if (clickedButton == null) return;

            // Lấy nội dung của nút để phân biệt hành động
            string buttonText = clickedButton.Content.ToString();

            // Nếu nút "Cancel Bill" được nhấn, hiển thị cảnh báo
            if (buttonText == "Cancel Bill")
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
                string customerPhone = CustomerNameTextBox.Text;

                // Lấy thông tin nhân viên từ TextBox
                int userId = int.Parse(UserIdTextBox.Text);
                string branchId = BranchComboBox.SelectedValue.ToString();

                // Lấy thông tin giảm giá từ TextBox
                int discount = 0;

                // Lấy giá trị của Tag từ ComboBox PaymentMethod
                var selectedPaymentMethod = PaymentMethod.SelectedItem as ComboBoxItem;
                int paymentMethod = selectedPaymentMethod != null
                    ? int.Parse(selectedPaymentMethod.Tag.ToString())
                    : 1; // Mặc định là 1 nếu không chọn được

                // Tạo danh sách sản phẩm với product_id
                var products = SelectedItems.Select(item => item.product_id).ToList();

                // Xác định bill_id từ hệ thống của bạn
                int billId = int.Parse(BillIdTextBox.Text);

                // Xác định đường dẫn API với bill_id
                string apiUrl = $"/bill/update?bill_id={billId}";

                // Xác định trạng thái dựa trên nút được nhấn
                int status = 0; // Mặc định là Cancel (nếu "Cancel Bill" được nhấn)
                if (buttonText == "Print")
                {
                    status = 2; // Trạng thái in bill
                }
                var parameters = new Dictionary<string, object>
                {
                    { "customer_name", customerName },
                    { "customer_phone", customerPhone },
                    { "branch_id", Int32.Parse(branchId) },
                    { "user_id", userId },
                    { "discount", discount },
                    { "pay_method", paymentMethod },
                    { "products", products },
                    {"status" , status}
                };
                string jsonContent = JsonConvert.SerializeObject(parameters, Formatting.Indented);
                
                
                // Gọi API để cập nhật bill
                var apiService = new Api();
                await apiService.PostApiAsync(apiUrl, jsonContent, (responseBody) =>
                {
                    string responseBodyJSON = JsonConvert.SerializeObject(responseBody, Formatting.Indented);
                    Console.WriteLine(responseBodyJSON);

                    try
                    {
                        var responseData =
                            JsonConvert.DeserializeObject<BranchApiResponse<BillResponseData>>(responseBody);
                        if (responseData != null && responseData.status == 200)
                        {
                            GetBills(); // Cập nhật lại danh sách bill

                            if (status == 2) // In hóa đơn nếu nhấn "Print"
                            {
                                FlowDocument document = CreateBillDocument();
                                PrintBill(document);
                                ClearOrderDetails();
                            }
                        }
                        else
                        {
                            Console.WriteLine($"API Error: {responseData?.message ?? "Unknown error"}");
                            MessageBox.Show($"API Error: {responseData?.message ?? "Unknown error"}", "API Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing response: {ex.Message}");
                        MessageBox.Show($"Error parsing response: {ex.Message}", "Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // Console.WriteLine("branchId in get BranchComboBox_SelectionChanged: " + branchId);
                DateTime fromDate = DateTime.Today;
                DateTime toDate = DateTime.Today;
                if (BranchComboBox.SelectedItem is Branch selectedBranch)
                {
                    Address = selectedBranch.address; // Lấy địa chỉ từ đối tượng Branch
                    // Console.WriteLine("Selected Branch Address: " + Address);
                    // Thực hiện các hành động khác với địa chỉ này, ví dụ hiển thị trên UI hoặc lưu trữ
                }

                GetListProduct(branchId);
                GetBills(fromDate, toDate, branchId);
            }
        }

        private async Task GetBills(DateTime? fromDate = null, DateTime? toDate = null, int branchId = 0,
            int? billId = null)
        {
            // Console.WriteLine("get bills is called");
            ShowLoading(true);

            try
            {
                // Sử dụng branchId được truyền vào hoặc mặc định là BranchId
                branchId = branchId > 0 ? branchId : BranchId;
                fromDate ??= DateTime.Today;
                toDate ??= DateTime.Today;

                // Lấy offset của múi giờ cục bộ so với UTC
                TimeSpan offset = TimeZoneInfo.Local.BaseUtcOffset;
                // Console.WriteLine("gmt: " + offset.TotalHours + ':' + offset.Minutes);

                // Điều chỉnh fromDate và toDate dựa trên offset của múi giờ cục bộ
                DateTime adjustedFromDate = fromDate.Value.Add(-offset);
                DateTime adjustedToDate = toDate.Value.Add(-offset).AddHours(23).AddMinutes(59).AddSeconds(59);

                // Chuyển đổi fromDate và toDate thành chuỗi theo định dạng yêu cầu
                string startDay = adjustedFromDate.ToString("yyyy-MM-dd HH:mm:ss");
                string endDay = adjustedToDate.ToString("yyyy-MM-dd HH:mm:ss");
                // Console.WriteLine("startDay: " + startDay);
                // Console.WriteLine("endDay: " + endDay);

                // Xây dựng URL API
                string apiUrl = "/bill/get";

                // Xây dựng nội dung yêu cầu POST
                var parameters = new Dictionary<string, string>
                {
                    { "branch_id", branchId.ToString() },
                    { "start_day", startDay},
                    { "end_day", endDay },
                    { "status", "-1" }
                };

                if (billId.HasValue)
                {
                    parameters.Add("bill_id", billId.Value.ToString());
                }

                Console.WriteLine("Parameters for API request:");
                foreach (var param in parameters)
                {
                    Console.WriteLine($"{param.Key}: {param.Value}");
                }

                var apiService = new Api();

                // Gọi PostApiAsync để gửi yêu cầu
                await apiService.PostApiAsync(apiUrl, parameters, (responseBody) =>
                {
                    try
                    {
                        var responseData = JsonConvert.DeserializeObject<BillListRespon>(responseBody);
                        // var responseconvert = JsonConvert.DeserializeObject<dynamic>(responseBody);
                        // Console.WriteLine("responseconvert in get Bill in bill list");
                        // Console.WriteLine(responseconvert);

                        if (responseData != null && responseData.status == 200)
                        {
                            Bill.Clear();
                            TotalMoney.Text = (responseData.data.total_price != 0 ? responseData.data.total_price : 0) +
                                              " $";
                            TotalCredit.Text =
                                (responseData.data.total_credit != 0 ? responseData.data.total_credit : 0) + " $";
                            TotalCash.Text = (responseData.data.total_cash != 0 ? responseData.data.total_cash : 0) +
                                             " $";
                            TotalBill.Text = (responseData.data.total_bill != 0 ? responseData.data.total_bill : 0)
                                .ToString();
                            foreach (var bill in responseData.data.list)
                            {
                                // Console.WriteLine("bill.customer_phone: " + bill.customer_phone);
                                Bill.Add(new BillFromList
                                {
                                    bill_id = bill.bill_id,
                                    customer_name = bill.customer_name,
                                    customer_phone = bill.customer_phone,
                                    branch_id = bill.branch_id,
                                    pay_method = bill.pay_method,
                                    user_id = bill.user_id,
                                    total_price = bill.total_price,
                                    discount = bill.discount,
                                    status = bill.status,
                                });
                            }

                            FilteredBill.Refresh();
                        }
                        else
                        {
                            MessageBox.Show($"API Error: {responseData?.message ?? "Unknown error"}");
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
                Console.WriteLine($"Error fetching bills: {ex.Message}");
                MessageBox.Show($"Error fetching bills: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }


        private void BillIdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Gọi phương thức xử lý nút Search khi nhấn phím Enter
                SearchButton_Click(sender, e);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Lấy giá trị bill ID từ TextBox
            int.TryParse(BillIdSearch.Text, out int billId);

            // Nếu nhập ID bill hợp lệ, gọi GetBills với billId
            if (billId > 0)
            {
                await GetBills(billId: billId);
                UpdateProductSelectionState();
            }
            else
            {
                MessageBox.Show("Please enter a valid Bill ID.", "Invalid Input", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            BillIdTextBox.Text = "";
            // Gọi GetBills mà không truyền ID bill để lấy toàn bộ danh sách
            UpdateProductSelectionState();
            await GetBills();
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
                string formattedPrice = price % 1 == 0 ? price.ToString("N0") : price.ToString("N2");

                document.Blocks.Add(CreateLineRow(new[]
                {
                    index.ToString(),
                    item.product_name,
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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
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
            // if (printDialog.ShowDialog() == true)
            // {
            //     IDocumentPaginatorSource idpSource = document;
            //     printDialog.PrintDocument(idpSource.DocumentPaginator, "Bill Print");
            // }
            var printQueue = printDialog.PrintQueue;
            XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(printQueue);
            writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);
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
                    permision = user.Permission;
                    if (permision == "1")
                    {
                        ProductBorder.Visibility = Visibility.Collapsed;
                        BillListScroll.Height = 520;
                    }
                    else
                    {
                        ProductBorder.Visibility = Visibility.Visible;
                    }

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
            UpdateProductSelectionState();
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