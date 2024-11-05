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

        private bool _isPasswordVisible = false;
        public ObservableCollection<ListBillRespone> Bill { get; set; }
        public ICollectionView FilteredBill { get; set; }
        public ObservableCollection<UserFromListApi> Users { get; set; }
        public ICollectionView FilteredUsers { get; set; }
        public ObservableCollection<ProductInBillModel> SelectedItems { get; set; }
        public ObservableCollection<Branch> Branches { get; set; }
        private WpfSvgWindow _wpfWindow;
        private ApiConnect _apiConnect = new ApiConnect();
        public ObservableCollection<Product> Products { get; set; }
        public int BranchId { get; set; }
        public string Address { get; set; }
        public ICollectionView FilteredProducts { get; set; }
        private bool _isInitialized = false;

        private ListBillRespone _selectedBill;
        private bool _isProductSelected = false;
        private Branch currentBranch;

        public ListBillRespone SelectedBill
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

        public event PropertyChangedEventHandler PropertyChanged;
        private User userLocal { get; set; }
        public TabBillList()
        {
            InitializeComponent();
            Bill = new ObservableCollection<ListBillRespone>();
            Products = new ObservableCollection<Product>(); // Khởi tạo Products
            FilteredBill = CollectionViewSource.GetDefaultView(Bill);
            FilteredProducts = CollectionViewSource.GetDefaultView(Products);
            SelectedItems = new ObservableCollection<ProductInBillModel>();
            Users = new ObservableCollection<UserFromListApi>(); // Khởi tạo ObservableCollection cho Users
            Branches = new ObservableCollection<Branch>(); // Khởi tạo ObservableCollection cho Branches
            var wpfSettings = new WpfDrawingSettings();
            var wpfRenderer = new WpfDrawingRenderer(wpfSettings);
            _wpfWindow = new WpfSvgWindow(500, 500, wpfRenderer);
            DataContext = this;
            IsBillSelected = false;
            GetBrandList();
            MainContentBorder.Visibility = Visibility.Collapsed;
            DatePickerInputFrom.SelectedDate = DateTime.Today;
            DatePickerInputTo.SelectedDate = DateTime.Today;

            GetStaffList();
            TotalPrice.Text = "0.00 $";
        
        }

        private async void GetStaffList()
        {
            ShowLoading(true);
            var user = await DatabaseHelper.GetUserAsync();
            userLocal = user;
            string url = $"/user/list?branch_id={user.BranchId}";
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
                        // if (StaffComboBox.Items.Count > 0)
                        // {
                        //     StaffComboBox.SelectedIndex = 0;
                        // }
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
                        // Dispatcher.Invoke(() =>
                        // {
                        //     BranchComboBox.ItemsSource = responseData.data;
                        //     BranchComboBox.DisplayMemberPath = "name";
                        //     BranchComboBox.SelectedValuePath = "branch_id";
                        //     BranchComboBox.SelectedValue =
                        //         BranchId > 0 ? BranchId : 1; // Đặt giá trị mặc định là branch_id = 1
                        // });
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
            if (sender is Border border && border.DataContext is ListBillRespone selectedBill)
            {
                SelectedBill = selectedBill;

                // Cập nhật IsBillSelected dựa trên trạng thái của hóa đơn
                // IsBillSelected = selectedBill.status == 1; // Chỉ cho phép chọn nếu status là PROCESSING

                // IsProductSelected = IsBillSelected; // Chỉ cho phép nhấn nút Plus nếu hóa đơn đang xử lý

                // Xây dựng URL API và tham số
                // string apiUrl = "/bill/branch/detail";
                var parameters = new Dictionary<string, string>
                    { };
                string param = JsonConvert.SerializeObject(parameters, Formatting.Indented);
                // Console.WriteLine("parameters");
                // Console.WriteLine(param);
                var apiService = new Api();
                try
                {
                    // Console.WriteLine($"{apiUrl}?bill_id={selectedBill.bill_id.ToString()}");
                    // Gọi phương thức GetApiAsync để thực hiện yêu cầu GET
                    var selectedEmployee = StaffComboBox.SelectedItem as UserFromListApi;
                    int? employeeId = selectedEmployee?.user_id;
                    var apiUrl = employeeId != null
                        ? $"/bill/user/detail?bill_id={selectedBill.bill_id.ToString()}&user_id={employeeId.ToString()}"
                        : $"/bill/branch/detail?bill_id={selectedBill.bill_id.ToString()}&user_id=-1";
                    await apiService.GetApiAsync(apiUrl, parameters,
                        (responseBody) =>
                        {
                            try
                            {
                                var responseData =
                                    JsonConvert.DeserializeObject<BranchApiResponse<BilLResponBranch>>(responseBody);
                                Console.WriteLine("responseData: " + Utls.FormatJsonString(responseBody));
                                if (responseData != null && responseData.status == 200)
                                {
                                    var billDetail = responseData.data;

                                    // Cập nhật UI với thông tin từ billDetail
                                    CustomerNameTextBox.Text = billDetail.customer_name;
                                    PhoneNumberTextBox.Text = billDetail.customer_phone;
                                    TotalPrice.Text = $"{billDetail.total_price} $";
                                    SelectedItems.Clear();
                                    string productsJson =
                                        JsonConvert.SerializeObject(billDetail, Formatting.Indented);
                                    Console.WriteLine("Bill Detail Products:");
                                    Console.WriteLine(productsJson);
                                    foreach (var product in billDetail.service)
                                    {
                                        // Kiểm tra độ dài của product_name trước khi cắt
                                        // string displayName = product.product_name.Length > 25
                                        //     ? product.product_name.Substring(0, 25) + "..."
                                        //     : product.product_name;

                                        SelectedItems.Add(new ProductInBillModel()
                                        {
                                            product_name = product.product_name,
                                            price = product.price,
                                            bill_id = product.bill_id,
                                            user_id = product.user_id,
                                            user_name = ShowNameEmployee(product.user_id),
                                            bill_detail_id = product.bill_detail_id,
                                            service_fee = product.service_fee
                                        });
                                    }

                                    TotalPriceChanged();
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

        private string ShowNameEmployee(int employeID)
        {
            var EmployeeIsFilted = Users.FirstOrDefault(x => x.user_id == employeID);
            return EmployeeIsFilted.name;
        }

        private void PrintListBill_Click(object sender, RoutedEventArgs e)
        {
            if (FilteredBill == null || FilteredBill.IsEmpty)
            {
                MessageBox.Show("No bills to print.");
                return;
            }

            FlowDocument document = new FlowDocument
            {
                PageWidth = 275,
                PagePadding = new Thickness(10),
                ColumnWidth = double.PositiveInfinity
            };

            // Thêm tiêu đề
            Paragraph title = new Paragraph(new Run("Bill List"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(title);
            var employee = StaffComboBox.SelectedItem as UserFromListApi;
            var employeeName = employee?.name;
            Paragraph Name = new Paragraph(new Run(employeeName != null ? employeeName : userLocal.Name))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(Name);
            Paragraph FromDate = new Paragraph(new Run("From: " + DatePickerInputFrom.SelectedDate.ToString()))
            {
                FontSize = 14,
                FontWeight = FontWeights.Light,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(FromDate);
            Paragraph ToDate = new Paragraph(new Run("To: " + DatePickerInputTo.SelectedDate.ToString()))
            {
                FontSize = 14,
                FontWeight = FontWeights.Light,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(ToDate);

            // Thêm tiêu đề bảng
            document.Blocks.Add(CreateHeaderRow(new[] { "Bill ID", "Customer", "Total ($)" }));

            // Thêm các dòng hóa đơn
            int index = 1;
            foreach (ListBillRespone bill in FilteredBill)
            {
                document.Blocks.Add(CreateLineRow(new[]
                {
                    bill.bill_id.ToString(),
                    bill.customer_name,
                    bill.total_price.ToString("N2"),
                }));
                index++;
            }

            Paragraph totalBill = new Paragraph(new Run("Total Bill: " + TotalBill.Text))
            {
                FontSize = 14,
                FontWeight = FontWeights.Light,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(totalBill);
            Paragraph totalPrice = new Paragraph(new Run("Total Price: " + TotalMoney.Text))
            {
                FontSize = 14,
                FontWeight = FontWeights.Light,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(totalPrice);
            Paragraph totalProfit = new Paragraph(new Run("Total Profit: " + EmployeeProfit.Text ))
            {
                FontSize = 14,
                FontWeight = FontWeights.Light,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(totalProfit);
            Paragraph totalSerivceFee = new Paragraph(new Run("Total Service Fee: " + TotalServiceFee.Text))
            {
                FontSize = 14,
                FontWeight = FontWeights.Light,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(totalSerivceFee);
            // Thực hiện in tài liệu
            PrintBill(document);
        }

        private void TotalPriceChanged()
        {
            double total = 0.0;
            foreach (var item in SelectedItems)
            {
                total += item.price + item.service_fee; // Cộng giá của mỗi sản phẩm
            }

            TotalPrice.Text = total.ToString("F2") + " $"; // Định dạng số thành chuỗi có 2 chữ số thập phân
        }

        private async Task GetBills(DateTime? fromDate = null, DateTime? toDate = null, int? billId = null,
            int? employeeId = null)
        {
            // Console.WriteLine("get bills is called");
            ShowLoading(true);

            try
            {
                // Sử dụng branchId được truyền vào hoặc mặc định là BranchId
                var user = await DatabaseHelper.GetUserAsync();
                var branchId = user.BranchId;
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
                string apiUrl = "/bill/branch/get";

                // Xây dựng nội dung yêu cầu POST
                var parameters = new Dictionary<string, string>
                {
                    { "branch_id", branchId.ToString() },
                    { "start_day", startDay },
                    { "end_day", endDay },
                    { "status", "-1" }
                };

                if (billId.HasValue)
                {
                    parameters.Add("bill_id", billId.Value.ToString());
                }

                if (employeeId.HasValue)
                {
                    parameters.Add("user_id", employeeId.Value.ToString());
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
                        var responseData = JsonConvert.DeserializeObject<ListBillModel>(responseBody);
                        var responseconvert = JsonConvert.DeserializeObject<dynamic>(responseBody);
                        Console.WriteLine("responseconvert in get Bill in bill list");
                        Console.WriteLine(responseconvert);

                        if (responseData != null && responseData.status == 200)
                        {
                            Bill.Clear();
                            TotalMoney.Text = (responseData.data.total_price != 0 ? responseData.data.total_price : 0) +
                                              " $";
                            EmployeeProfit.Text =
                                (responseData.data.total_profit != 0 ? responseData.data.total_profit : 0) + " $";

                            TotalBill.Text = (responseData.data.total_bill != 0 ? responseData.data.total_bill : 0)
                                .ToString();
                            
                            TotalServiceFee.Text = (responseData.data.total_service_fee != 0 ? responseData.data.total_service_fee : 0) + " $";
                            foreach (var bill in responseData.data.list)
                            {
                                // Console.WriteLine("bill.customer_phone: " + bill.customer_phone);
                                var status_convert = FormatStatus(bill.status);
                                // Console.WriteLine("status_convert: " + status_convert);
                                Bill.Add(new ListBillRespone
                                {
                                    bill_id = bill.bill_id,
                                    customer_name = bill.customer_name,
                                    customer_phone = bill.customer_phone,
                                    branch_id = bill.branch_id,
                                    // pay_method = bill.pay_method,
                                    // user_id = bill.user_id,
                                    total_price = bill.total_price,
                                    // discount = bill.discount,
                                    status = status_convert,
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

        private string FormatStatus(string status)
        {
            return status == "1" ? "Success" : "Cancel";
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
            // Lấy giá trị từ TextBox Bill ID
            int.TryParse(BillIdSearch.Text, out int billId);

            // Lấy ID của nhân viên từ ComboBox nếu có lựa chọn
            var selectedEmployee = StaffComboBox.SelectedItem as UserFromListApi;
            int? employeeId = selectedEmployee?.user_id;

            // Lấy giá trị từ DatePicker
            DateTime? fromDate = DatePickerInputFrom.SelectedDate;
            DateTime? toDate = DatePickerInputTo.SelectedDate;

            // Gọi GetBills với các tham số vừa lấy
            await GetBills(fromDate: fromDate, toDate: toDate, billId: billId > 0 ? billId : null,
                employeeId: employeeId);

            // UpdateProductSelectionState();
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            BillIdSearch.Text = string.Empty;
            CustomerNameTextBox.Text = string.Empty;
            PhoneNumberTextBox.Text = string.Empty;
            // Đặt ComboBox về giá trị ban đầu
            StaffComboBox.SelectedIndex = -1;

            // Xóa giá trị trong DatePicker
            DatePickerInputFrom.SelectedDate = DateTime.Today;
            DatePickerInputTo.SelectedDate = DateTime.Today;

            // Xóa danh sách sản phẩm đã chọn
            SelectedItems.Clear();

            // Cập nhật lại tổng giá
            TotalPrice.Text = "0.00 $"; // Hoặc giá trị mặc định

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

        private async void CancelBill(object sender, RoutedEventArgs e)
        {
            if (SelectedBill == null)
            {
                MessageBox.Show("Please select a bill to cancel.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ShowLoading(true); // Hiển thị loading trong quá trình gọi API
            string apiUrl = "/bill/delete?bill_id=" + SelectedBill.bill_id;
            // var parameters = new Dictionary<string, string>
            // {
            //     { "bill_id", SelectedBill.bill_id.ToString() }
            // };
            // Console.WriteLine("Parameters for API request cancel:");
            // foreach (var param in parameters)
            // {
            //     Console.WriteLine($"{param.Key}: {param.Value}");
            // }
            try
            {
                var apiService = new Api();
                await apiService.PostApiAsyncWithoutParam(apiUrl, (responseBody) =>
                {
                    var responseData =
                        JsonConvert.DeserializeObject<APIResponFromCancelBill>(
                            responseBody); // Đảm bảo `ApiResponse` là lớp phù hợp để phân tích kết quả.
                    var responseconvert = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    Console.WriteLine("responseconvert when cancel");
                    Console.WriteLine(responseconvert);
                    if (responseData != null && responseData.status == 200)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // Xóa hóa đơn khỏi danh sách và làm mới giao diện
                            // Bill.Remove(SelectedBill);
                            FilteredBill.Refresh();
                            SelectedBill = null;
                            GetBills();
                            MessageBox.Show("Bill has been successfully canceled.", "Success", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        });
                    }
                    else
                    {
                        MessageBox.Show($"Failed to cancel the bill: {responseData?.message ?? "Unknown error"}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false); // Ẩn loading khi hoàn tất
            }
        }

        private FlowDocument CreateBillDocument(string billId)
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

            Paragraph name =
                new Paragraph(new Run(
                    currentBranch.name))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(name);
            Paragraph desc =
                new Paragraph(new Run(
                    currentBranch.description))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(desc);
            Paragraph website =
                new Paragraph(new Run(
                    currentBranch.website))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(website);
            // Title
            Paragraph title = new Paragraph(new Run("Bill Invoice (COMPLETE)"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(title);


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

            Paragraph address =
                new Paragraph(new Run(
                    currentBranch.address))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(address);
            Paragraph phone =
                new Paragraph(new Run(
                    currentBranch.sdt))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(phone);

            Paragraph time =
                new Paragraph(new Run(
                    currentBranch.open_close))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(time);
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

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

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

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

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

            var printQueue = printDialog.PrintQueue;
            XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(printQueue);
            writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);
        }


        private void ClearOrderDetails()
        {
            // UserIdTextBox.Text = string.Empty;
            // StaffTextBox.Text = string.Empty;
            CustomerNameTextBox.Text = string.Empty;
            PhoneNumberTextBox.Text = string.Empty;
            // BillIdTextBox.Text = string.Empty;
            TotalPrice.Text = string.Empty;

            SelectedItems.Clear();
        }

        private void OnClearOrderDetailsClick(object sender, RoutedEventArgs e)
        {
            // Xóa các giá trị trong các TextBox
            ClearOrderDetails();
            // UpdateProductSelectionState();
        }

        private void ShowLoading(bool show)
        {
            Dispatcher.Invoke(() => { LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed; });
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                                Console.WriteLine("Response from login");
                                Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));

                                try
                                {
                                    // Tạo đối tượng người dùng mới từ dữ liệu đăng nhập
                                    var User = new User
                                    {
                                        UserName = data.user_name,
                                        AccessToken = data.access_token,
                                        Name = data.name,
                                        UserId = data.user_id,
                                        Permission = data.permision,
                                        BranchId = data.branch_id
                                    };

                                    // Ẩn form đăng nhập và hiện nội dung Border chính
                                    GetBills();
                                    LoginFormBorder.Visibility = Visibility.Collapsed;
                                    MainContentBorder.Visibility = Visibility.Visible;

                                    LoginLoadingIcon.Visibility = Visibility.Collapsed;
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
    }
}