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

        private BilLResponBranch _selectedBillDetail;

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

            MainContentBorder.Visibility = Visibility.Collapsed;
            DatePickerInputFrom.SelectedDate = DateTime.Today; // 0 giờ ngày hôm nay
            DatePickerInputTo.SelectedDate = DateTime.Today.AddDays(1).AddTicks(-1); // 23:59:59 ngày hôm nay

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
            GetBrandList(user.BranchId);
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

        private async void GetBrandList(int user_branch_id)
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
                        var listBranch = responseData.data;
                        var filterBranch = listBranch.Find(branch => branch.branch_id == user_branch_id);
                        currentBranch = filterBranch;
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
                // ShowLoading(true);
                // for (int i = 0; i < VisualTreeHelper.GetChildrenCount(MyItemsControl); i++)
                // {
                //     var container = VisualTreeHelper.GetChild(MyItemsControl, i);
                //
                //     // Kiểm tra nếu phần tử con là Border
                //     if (container is Border border2)
                //     {
                //         border2.Background = Brushes.Transparent; // Reset màu nền
                //     }
                // }
                //
                // SelectedBill = selectedBill;
                // if (sender is Border clickedBorder)
                // {
                //     clickedBorder.Background = Brushes.Green; // Đổi màu nền
                // }

                var parameters = new Dictionary<string, string> { };
                var apiService = new Api();


                // Console.WriteLine("bill id click: " + selectedBill.bill_id);
                // Console.WriteLine("bill id is selected: " + selectedBill.IsSelected);

                try
                {
                    var selectedEmployee = StaffComboBox.SelectedItem as UserFromListApi;
                    int? employeeId = selectedEmployee?.user_id;
                    var apiUrl = employeeId != null
                        ? $"/bill/user/detail?bill_id={selectedBill.bill_id}&user_id={employeeId}"
                        : $"/bill/branch/detail?bill_id={selectedBill.bill_id}&user_id=-1";

                    await apiService.GetApiAsync(apiUrl, parameters, (responseBody) =>
                    {
                        var responseData =
                            JsonConvert.DeserializeObject<BranchApiResponse<BilLResponBranch>>(responseBody);
                        // Console.WriteLine("get bill detail");
                        // Console.WriteLine(Utls.FormatJsonString(responseBody));
                        if (responseData != null && responseData.status == 200)
                        {
                            _selectedBillDetail = responseData.data; // Lưu dữ liệu trả về vào biến
                            _selectedBillDetail.bill_id = selectedBill.bill_id;
                            CustomerNameTextBox.Text = _selectedBillDetail.customer_name;
                            PhoneNumberTextBox.Text = _selectedBillDetail.customer_phone;
                            TotalPrice.Text = $"{_selectedBillDetail.total_price} $";
                            SelectedItems.Clear();

                            foreach (var product in _selectedBillDetail.service)
                            {
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
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fetching bill details: {ex.Message}");
                }
                // finally
                // {
                //     ShowLoading(false);
                // }
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
            Paragraph FromDate =
                new Paragraph(new Run("From Date: " +
                                      (DatePickerInputFrom.SelectedDate?.ToString("MM/dd/yyyy") ??
                                       DateTime.Now.ToString("MM/dd/yyyy"))))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Light,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(FromDate);

            Paragraph ToDate =
                new Paragraph(new Run("To Date: " +
                                      (DatePickerInputTo.SelectedDate?.ToString("MM/dd/yyyy") ??
                                       DateTime.Now.ToString("MM/dd/yyyy"))))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Light,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(ToDate);

            document.Blocks.Add(CreateHeaderRow(new[] { "Bill ID", "Customer", "Status", "Total ($)" }));
            int index = 1;
            foreach (ListBillRespone bill in FilteredBill)
            {
                document.Blocks.Add(CreateLineRow(new[]
                {
                    bill.bill_id.ToString(),
                    bill.customer_name,
                    bill.status,
                    // FormatStatus(bill.status),
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
            Paragraph totalPriceWithSupply = new Paragraph(new Run("Total Price + Service: " + TotalMoney.Text))
            {
                FontSize = 14,
                FontWeight = FontWeights.Light,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(totalPriceWithSupply);
            // var totalPriceWithoutSupplyNum = double.Parse(TotalMoney.Text.Replace("$", "").Trim()) 
            //                                  - double.Parse(TotalServiceFee.Text.Replace("$", "").Trim());

            Paragraph totalPriceWithoutSupply =
                new Paragraph(new Run("Total Price - Service: " + TotalMoneyWithoutService.Text))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Light,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(totalPriceWithoutSupply);
            Paragraph totalProfit = new Paragraph(new Run("Total Profit: " + EmployeeProfit.Text))
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
            // PreviewBill(document);
            // Thực hiện in tài liệu
            PrintBill(document);
        }

        private void PrintRevenue_Click(object sender, RoutedEventArgs e)
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
            Paragraph FromDate =
                new Paragraph(new Run("From Date: " +
                                      (DatePickerInputFrom.SelectedDate?.ToString("MM/dd/yyyy") ??
                                       DateTime.Now.ToString("MM/dd/yyyy"))))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Light,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(FromDate);

            Paragraph ToDate =
                new Paragraph(new Run("To Date: " +
                                      (DatePickerInputTo.SelectedDate?.ToString("MM/dd/yyyy") ??
                                       DateTime.Now.ToString("MM/dd/yyyy"))))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Light,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(ToDate);

            Paragraph totalBill = new Paragraph(new Run("Total Bill: " + TotalBill.Text))
            {
                FontSize = 14,
                FontWeight = FontWeights.Light,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(totalBill);
            Paragraph totalPriceWithSupply = new Paragraph(new Run("Total Price + Service: " + TotalMoney.Text))
            {
                FontSize = 14,
                FontWeight = FontWeights.Light,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Roboto"),
            };
            document.Blocks.Add(totalPriceWithSupply);
            // var totalPriceWithoutSupplyNum = double.Parse(TotalMoney.Text.Replace("$", "").Trim()) 
            //                                  - double.Parse(TotalServiceFee.Text.Replace("$", "").Trim());

            Paragraph totalPriceWithoutSupply =
                new Paragraph(new Run("Total Price - Service: " + TotalMoneyWithoutService.Text))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Light,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(totalPriceWithoutSupply);
            Paragraph totalProfit = new Paragraph(new Run("Total Profit: " + EmployeeProfit.Text))
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
            // PreviewBill(document);
            // Thực hiện in tài liệu
            PrintBill(document);
        }

        private void PreviewBill(FlowDocument document)
        {
            Window previewWindow = new Window
            {
                Title = "Bill Preview",
                Width = 300,
                Height = 1000,
                Content = new FlowDocumentScrollViewer
                {
                    Document = document,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible
                }
            };
            previewWindow.ShowDialog();
        }

        private void PrintDetailBill(object sender, RoutedEventArgs e)
        {
            if (_selectedBillDetail == null)
            {
                MessageBox.Show("Please select at least one item to print bill.");
                return;
            }

            FlowDocument billDocument = CreateBillDetailDocument();

            // Hiển thị bản xem trước hóa đơn trước khi in
            //PreviewBill(billDocument);

            // Thực hiện in tài liệu chi tiết bill
            PrintBill(billDocument);
        }

        private FlowDocument CreateBillDetailDocument()
        {
            if (_selectedBillDetail == null) return null;

            string customerName = _selectedBillDetail.customer_name;
            string phoneNumber = _selectedBillDetail.customer_phone;
            double totalPrice = _selectedBillDetail.total_price;
            int billID = _selectedBillDetail.bill_id ?? 0;
            FlowDocument document = new FlowDocument
            {
                PageWidth = 275,
                PagePadding = new Thickness(10),
                ColumnWidth = double.PositiveInfinity
            };

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
                document.Blocks.Add(new BlockUIContainer(svgImage));
            }

            document.Blocks.Add(new Paragraph(new Run(currentBranch.name))
                { FontSize = 10, TextAlignment = TextAlignment.Center });
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


            document.Blocks.Add(
                new Paragraph(new Run($"Bill ID: #{billID}\nDate: {_selectedBillDetail.created_at:dd/MM/yyyy HH:mm}"))
                {
                    FontSize = 12,
                    TextAlignment = TextAlignment.Left,
                    FontFamily = new FontFamily("Roboto"),
                });

            document.Blocks.Add(CreateDetailHeaderRow(new[] { "Employee", "Price", "Fee", "Total" }));

            foreach (var item in _selectedBillDetail.service)
            {
                document.Blocks.Add(CreateDetailLineRow(new[]
                {
                    ShowNameEmployee(item.user_id),
                    item.price.ToString("N2"),
                    item.service_fee.ToString("N2"),
                    (item.price + item.service_fee).ToString("N2"),
                }));
            }

            document.Blocks.Add(new Paragraph(new Run($"\nTotal: {totalPrice} $"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
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


        private BlockUIContainer CreateDetailHeaderRow(string[] columns)
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

        private BlockUIContainer CreateDetailLineRow(string[] columns)
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
                // TimeSpan offset = TimeZoneInfo.Local.BaseUtcOffset;
                // Console.WriteLine("gmt: " + offset.TotalHours + ':' + offset.Minutes);

                // Điều chỉnh fromDate và toDate dựa trên offset của múi giờ cục bộ
                DateTime adjustedFromDate = fromDate.Value;
                DateTime adjustedToDate = toDate.Value;

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
                        Console.WriteLine(Utls.FormatJsonString(responseBody));
                        var employee = StaffComboBox.SelectedItem as UserFromListApi;
                        var employeeName = employee?.name;
                        if (responseData != null && responseData.status == 200)
                        {
                            Bill.Clear();
                            TotalMoney.Text = (responseData.data.total_price != 0 ? responseData.data.total_price : 0) +
                                              " $";
                            EmployeeProfit.Text =
                                (responseData.data.total_profit != 0
                                    ? (employeeName == null
                                        ? Math.Round(responseData.data.total_profit - responseData.data.total_service_fee, 2)
                                        : Math.Round(responseData.data.total_profit, 2)
                                    )
                                    : 0).ToString("F2") + " $";

                            TotalBill.Text = (responseData.data.total_bill != 0 ? responseData.data.total_bill : 0)
                                .ToString();

                            TotalServiceFee.Text = (responseData.data.total_service_fee != 0
                                ? responseData.data.total_service_fee
                                : 0) + " $";
                            TotalMoneyWithoutService.Text = ((responseData.data.total_price != 0 &&
                                                              responseData.data.total_profit != 0)
                                ? responseData.data.total_price - responseData.data.total_service_fee
                                : 0) + " $";
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
                                    service_fee = bill.service_fee,
                                    total_price = bill.total_price,
                                    price = bill.price,
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
            if (_selectedBillDetail == null)
            {
                MessageBox.Show("Please select a bill to cancel.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Hiển thị thông báo xác nhận
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to cancel this bill?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                // Nếu người dùng chọn "No", thoát hàm
                return;
            }

            // Console.WriteLine("SelectedBill.bill_id: " + _selectedBillDetail.bill_id);
            // Console.WriteLine("SelectedBill.bill_id: " + SelectedBill.bill_id);
            string apiUrl = "/bill/delete?bill_id=" + _selectedBillDetail.bill_id;
            ShowLoading(true); // Hiển thị loading trong quá trình gọi API
            

            try
            {
                var apiService = new Api();
                await apiService.PostApiAsyncWithoutParam(apiUrl, (responseBody) =>
                {
                    var responseData = JsonConvert.DeserializeObject<APIResponFromCancelBill>(responseBody);
                    var responseconvert = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    Console.WriteLine("responseconvert when cancel");
                    Console.WriteLine(responseconvert);
            
                    if (responseData != null && responseData.status == 200)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // Xóa hóa đơn khỏi danh sách và làm mới giao diện
                            // Bill.Remove(SelectedBill);
                            // FilteredBill.Refresh();
                            SelectedBill = null;
                            MessageBox.Show("Bill has been successfully canceled.", "Success", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            // GetBills();
                            int.TryParse(BillIdSearch.Text, out int billId);

                            // Lấy ID của nhân viên từ ComboBox nếu có lựa chọn
                            var selectedEmployee = StaffComboBox.SelectedItem as UserFromListApi;
                            int? employeeId = selectedEmployee?.user_id;

                            // Lấy giá trị từ DatePicker
                            DateTime? fromDate = DatePickerInputFrom.SelectedDate;
                            DateTime? toDate = DatePickerInputTo.SelectedDate;

                            // Gọi GetBills với các tham số vừa lấy
                            GetBills(fromDate: fromDate, toDate: toDate, billId: billId > 0 ? billId : null,
                                employeeId: employeeId);
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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(43) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(73) });

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

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(43) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(73) });

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
            CustomerNameTextBox.Text = string.Empty;
            PhoneNumberTextBox.Text = string.Empty;
            TotalPrice.Text = string.Empty;
            SelectedItems.Clear();
        }

        private void OnClearOrderDetailsClick(object sender, RoutedEventArgs e)
        {
            // Xóa các giá trị trong các TextBox
            ClearOrderDetails();
            _selectedBillDetail = null;
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