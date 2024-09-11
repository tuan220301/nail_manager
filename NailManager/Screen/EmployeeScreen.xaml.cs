using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Xps;
using NailManager.Models;
using NailManager.Services;
using Newtonsoft.Json;

namespace NailManager.Screen
{
    public partial class EmployeeScreen : UserControl
    {
        public ObservableCollection<UserFromListApi> Users { get; set; }
        public ICollectionView FilteredUsers { get; set; }
        public int BranchId { get; set; }
        public string permision = "";
        private string _permisionDelete;
        private ApiConnect _apiConnect = new ApiConnect();

        public ObservableCollection<Bill> FilteredBills { get; set; } = new ObservableCollection<Bill>();

        public string PermisionDelete
        {
            get { return _permisionDelete; }
            set
            {
                _permisionDelete = value;
                OnPropertyChanged(nameof(PermisionDelete));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Biến để lưu trữ người dùng đang được chỉnh sửa
        private UserFromListApi _selectedUser;

        public EmployeeScreen()
        {
            InitializeComponent();

            // Khởi tạo Users và FilteredUsers
            Users = new ObservableCollection<UserFromListApi>();
            FilteredUsers = CollectionViewSource.GetDefaultView(Users);

            // Đặt DataContext
            DataContext = this;

            // Thiết lập ngày mặc định cho DatePicker
            DatePickerInputTo.SelectedDate = DateTime.Today;
            DatePickerInputFrom.SelectedDate = DateTime.Today;

            // Lấy thông tin người dùng và thiết lập giao diện
            GetUser();
            // Lấy danh sách chi nhánh
            GetBrandList();
            // Lấy danh sách người dùng
            GetListUser();
        }

        private async void OnUserItemClicked(object sender, MouseButtonEventArgs e)
        {
            // Tạo các biến để lưu trữ username và password
            string user_name = null;
            string password = null;

            // Nếu không phải admin thì hiển thị modal xác thực
            if (permision != "1")
            {
                AuthenticationWindow authWindow = new AuthenticationWindow();
                bool? dialogResult = authWindow.ShowDialog();

                // Nếu xác thực không thành công, thoát khỏi hàm
                if (dialogResult != true || !authWindow.IsAuthenticated)
                {
                    return;
                }

                // Lấy username và password từ cửa sổ xác thực
                user_name = authWindow.EnteredUsername;
                password = authWindow.EnteredPassword;
                // Console.WriteLine("Username: " + user_name);
                // Console.WriteLine("Password: " + password);
            }

            ShowLoading(true);

            if (sender is Border border && border.DataContext is UserFromListApi selectedUser)
            {
                // Gán các giá trị từ dòng được chọn vào các TextBox
                UserIDTextBox.Text = selectedUser.user_id.ToString();
                UserNameTextBox.Text = selectedUser.user_name;
                RateTextBox.Text = selectedUser.rate.ToString();
                PasswordTextBox.Text = ""; // Không hiển thị mật khẩu thực

                // Lưu trữ người dùng hiện tại để chỉnh sửa
                _selectedUser = selectedUser;

                // Hiển thị StackPanel chứa UserIDTextBox và mở rộng cột
                UserIDPanel.Visibility = Visibility.Visible;
                UserIDColumn.Width = new GridLength(200); // Hoặc kích thước bạn muốn

                // Đổi nội dung nút thành "Edit"
                SaveButton.Content = "Edit";

                // Thiết lập giá trị cho PermissionComboBox dựa trên giá trị permission
                if (selectedUser.permission == 2.ToString())
                {
                    PermissionComboBox.SelectedIndex = 0; // CASHIER
                }
                else if (selectedUser.permission == 3.ToString())
                {
                    PermissionComboBox.SelectedIndex = 1; // STAFF
                }
                else
                {
                    PermissionComboBox.SelectedIndex = -1; // Không chọn gì nếu permission không khớp
                }

                // Thiết lập thời gian bắt đầu và kết thúc cho ngày hiện tại
                TimeSpan offset = TimeZoneInfo.Local.BaseUtcOffset;
                DateTime fromDate = DateTime.Today;
                DateTime toDate = DateTime.Today;
                DateTime adjustedFromDate = fromDate.Add(-offset);
                DateTime adjustedToDate = toDate.Add(-offset).AddHours(23).AddMinutes(59).AddSeconds(59);

                // Chuyển đổi fromDate và toDate thành chuỗi theo định dạng yêu cầu
                string startDay = adjustedFromDate.ToString("yyyy-MM-dd HH:mm:ss");
                string endDay = adjustedToDate.ToString("yyyy-MM-dd HH:mm:ss");

                // Lấy user_id và branch_id
                int userId = selectedUser.user_id;
                int branchId = selectedUser.branch_id; // Giả sử branch_id có trong selectedUser

                // Xây dựng các tham số dưới dạng Dictionary<string, string>
                var parameters = new Dictionary<string, string>
                {
                    { "branch_id", branchId.ToString() },
                    { "user_id", userId.ToString() },
                    { "start_day", startDay },
                    { "end_day", endDay },
                    { "status", "1" }
                };
                Console.WriteLine("param:");
                // Thêm username và password vào parameters nếu có
                if (!string.IsNullOrEmpty(user_name) && !string.IsNullOrEmpty(password))
                {
                    parameters.Add("password", password);
                }

                // Console log các tham số
                foreach (var param in parameters)
                {
                    Console.WriteLine($"{param.Key}: {param.Value}");
                }

                // Tạo URL API
                string apiUrl = "/bill/get"; // Đường dẫn tương đối cho API
                var apiService = new Api();

                try
                {
                    // Gọi PostApiAsync để gửi yêu cầu
                    await apiService.PostApiAsync(apiUrl, parameters, (responseBody) =>
                    {
                        string formattedJson = JsonConvert.SerializeObject(responseBody, Formatting.Indented);
                        Console.WriteLine(formattedJson);
                        try
                        {
                            var responseData =
                                JsonConvert.DeserializeObject<BranchApiResponse<BillListResponse>>(responseBody);

                            // Console JSON đẹp và dễ đọc
                            Console.WriteLine("Response from API:");

                            if (responseData != null && responseData.status == 200)
                            {
                                // Cập nhật danh sách FilteredBills với dữ liệu mới từ API
                                FilteredBills.Clear();
                                foreach (var bill in responseData.data.list)
                                {
                                    FilteredBills.Add(bill);
                                }

                                // Cập nhật các trường khác nếu cần
                                TotalPriceText.Text = "Total Price: " + responseData.data.total_price;
                                TotalCashText.Text = "Total Cash: " + responseData.data.total_cash;
                                TotalCreditText.Text = "Total Credit: " + responseData.data.total_credit;
                                TotalProfitText.Text = "Total Profit: " + responseData.data.total_profit;
                                TotalBillText.Text = "Total Bill: " + responseData.data.total_bill;
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
                    Console.WriteLine($"Error calling API: {ex.Message}");
                    MessageBox.Show($"Error calling API: {ex.Message}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    ShowLoading(false);
                }
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
                    permision = user.Permission;
                    PermisionDelete = user.Permission;
                    Console.WriteLine("PermisionDelete: " + PermisionDelete);
                    if (permision == "1")
                    {
                        SaveButton.Visibility = Visibility.Visible;
                        PasswordPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        SaveButton.Visibility = Visibility.Collapsed;
                        PasswordPanel.Visibility = Visibility.Collapsed;
                    }

                    PrintBillButton.Visibility = Visibility.Visible;
                    PermissionGrid.Visibility = permision == "1" ? Visibility.Visible : Visibility.Collapsed;

                    if (user.BranchId > 0)
                    {
                        BranchId = user.BranchId;
                        BranchComboBox.Visibility = Visibility.Collapsed;
                        BorderBranch.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
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

        private async void GetListUser(int branchId = 1)
        {
            ShowLoading(true); // Hiển thị loading

            // Kiểm tra nếu BranchId có giá trị > 0 thì sử dụng nó
            if (BranchId > 0)
            {
                branchId = BranchId;
            }

            string url = $"/user/list?branch_id={branchId}";
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            var apiService = new Api();

            try
            {
                await apiService.GetApiAsync(url, parameters, (responseBody) =>
                {
                    // Kiểm tra xem API trả về có rỗng hay không
                    if (string.IsNullOrEmpty(responseBody))
                    {
                        MessageBox.Show("API response is empty or null.");
                        return;
                    }

                    var responseData =
                        JsonConvert.DeserializeObject<BranchApiResponse<List<UserFromListApi>>>(responseBody);

                    // Kiểm tra nếu việc parse JSON thất bại
                    if (responseData == null)
                    {
                        MessageBox.Show("Failed to parse API response.");
                        return;
                    }

                    // Nếu status là 200 và có dữ liệu trả về
                    if (responseData.status == 200 && responseData.data != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // Xóa dữ liệu cũ
                            Users.Clear();

                            // Thêm dữ liệu mới vào danh sách Users
                            foreach (var user in responseData.data)
                            {
                                Users.Add(user);
                            }

                            // Làm mới CollectionView để cập nhật UI
                            FilteredUsers.Refresh();
                        });
                    }
                    else
                    {
                        MessageBox.Show("Failed to load users.");
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing API response list user: {ex.Message}");
            }
            finally
            {
                ShowLoading(false); // Ẩn loading khi hoàn tất
            }
        }

        private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            ShowLoading(true); // Hiển thị loading
            try
            {
                // Lấy thông tin từ các trường nhập liệu
                string userName = UserNameTextBox.Text;
                string password = PasswordTextBox.Text;
                string rate = RateTextBox.Text;
                int branchId = BranchId > 0 ? BranchId : (int)BranchComboBox.SelectedValue;

                // Lấy giá trị được chọn từ ComboBox
                string permission = (PermissionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                // Tạo đối tượng ApiService
                var apiService = new Api();

                if (_selectedUser != null)
                {
                    // Chỉnh sửa người dùng hiện có
                    string url = "/user/update"; // Thay thế bằng endpoint API thực tế
                    var parameters = new Dictionary<string, string>
                    {
                        { "user_id", _selectedUser.user_id.ToString() }, // Sử dụng ID của người dùng đã chọn
                        { "user_name", userName },
                        { "password", password },
                        { "branch_id", branchId.ToString() },
                        { "rate", rate },
                        { "permission", permission == "CASHIER" ? "2" : "3" }
                    };

                    await apiService.PostApiAsync(url, parameters, (responseBody) =>
                    {
                        try
                        {
                            var responseData = JsonConvert.DeserializeObject<BranchApiResponse<string>>(responseBody);
                            if (responseData != null && responseData.status == 200)
                            {
                                GetListUser(branchId);
                                ClearInputFields(); // Xóa dữ liệu trên các ô nhập
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

                    // Reset sau khi chỉnh sửa
                    _selectedUser = null;
                    SaveButton.Content = "Save";
                }
                else
                {
                    // Tạo mới người dùng
                    string url = "/user/create"; // Thay thế bằng endpoint API thực tế
                    var parameters = new Dictionary<string, string>
                    {
                        { "name", userName },
                        { "user_name", userName },
                        { "password", password },
                        { "branch_id", branchId.ToString() },
                        { "rate", rate },
                        { "permission", permission == "CASHIER" ? "2" : "3" }
                    };
                    // string parametersJson = JsonConvert.SerializeObject(parameters, Formatting.Indented);
                    // Console.WriteLine("Parameters: " + parametersJson);

                    await apiService.PostApiAsync(url, parameters, (responseBody) =>
                    {
                        try
                        {
                            var responseData =
                                JsonConvert.DeserializeObject<BranchApiResponse<UserResponseData>>(responseBody);
                            if (responseData != null && responseData.status == 200)
                            {
                                GetListUser(branchId);
                                ClearInputFields(); // Xóa dữ liệu trên các ô nhập
                            }
                            else
                            {
                                Console.WriteLine($"API Error: {responseData?.message ?? "Unknown error"}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing API response create: {ex.Message}");
                        }
                    });
                }
            }
            finally
            {
                ShowLoading(false); // Ẩn loading khi hoàn tất
            }
        }

        private async void OnFilterButtonClick(object sender, RoutedEventArgs e)
        {
            ShowLoading(true); // Hiển thị loading

            try
            {
                // Lấy giá trị từ ComboBox Branch
                var selectedBranch = BranchComboBox.SelectedItem as Branch;
                int branchId = selectedBranch?.branch_id ?? 0;

                // Lấy giá trị từ ComboBox User
                int userId = _selectedUser?.user_id ?? 0;

                // Lấy giá trị từ DatePicker
                DateTime? fromDate = DatePickerInputFrom.SelectedDate;
                DateTime? toDate = DatePickerInputTo.SelectedDate;

                // Kiểm tra xem fromDate và toDate có giá trị không
                if (fromDate.HasValue && toDate.HasValue)
                {
                    // Lấy offset của múi giờ cục bộ so với UTC
                    TimeSpan offset = TimeZoneInfo.Local.BaseUtcOffset;

                    // Điều chỉnh fromDate và toDate dựa trên offset của múi giờ cục bộ
                    DateTime adjustedFromDate = fromDate.Value.Add(-offset);
                    DateTime adjustedToDate = toDate.Value.Add(-offset).AddHours(23).AddMinutes(59).AddSeconds(59);

                    // Chuyển đổi fromDate và toDate thành chuỗi theo định dạng yêu cầu
                    string startDay = adjustedFromDate.ToString("yyyy-MM-dd HH:mm:ss");
                    string endDay = adjustedToDate.ToString("yyyy-MM-dd HH:mm:ss");

                    Console.WriteLine("startDay: " + startDay);
                    Console.WriteLine("endDay: " + endDay);
                    Console.WriteLine("branchId: " + branchId);
                    Console.WriteLine("userId: " + userId);

                    // Xây dựng các tham số dưới dạng Dictionary<string, string>
                    var parameters = new Dictionary<string, string>
                    {
                        { "branch_id", branchId.ToString() },
                        { "user_id", userId.ToString() },
                        { "status", "1" }, // Giả định status là 1
                        { "start_day", startDay }, // Thêm startDay
                        { "end_day", endDay } // Thêm endDay
                    };

                    // Tạo URL API
                    string apiUrl = "/bill/get"; // Đường dẫn tương đối cho API
                    var apiService = new Api();

                    // Gọi GetApiAsync để gửi yêu cầu
                    await apiService.GetApiAsync(apiUrl, parameters, (responseBody) =>
                    {
                        try
                        {
                            var responseData =
                                JsonConvert.DeserializeObject<BranchApiResponse<List<Bill>>>(responseBody);

                            if (responseData != null && responseData.status == 200)
                            {
                                // Cập nhật danh sách FilteredBills với dữ liệu mới từ API
                                FilteredBills.Clear();
                                foreach (var bill in responseData.data)
                                {
                                    FilteredBills.Add(bill);
                                }
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
                else
                {
                    MessageBox.Show("Please select valid dates.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling API: {ex.Message}");
                MessageBox.Show($"Error calling API: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false); // Ẩn loading khi hoàn tất
            }
        }


        private async void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is UserFromListApi userToDelete)
            {
                // Hiển thị xác nhận trước khi xóa
                var result = MessageBox.Show($"Are you sure you want to delete user '{userToDelete.user_name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    ShowLoading(true); // Hiển thị loading

                    try
                    {
                        string url =
                            $"/user/delete?user_id={userToDelete.user_id}"; // Thay thế bằng endpoint API thực tế

                        var apiService = new Api();
                        await apiService.PostApiAsync(url, new Dictionary<string, string>(), (responseBody) =>
                        {
                            try
                            {
                                var responseData =
                                    JsonConvert.DeserializeObject<BranchApiResponse<string>>(responseBody);
                                if (responseData != null && responseData.status == 200)
                                {
                                    // Xóa người dùng khỏi danh sách
                                    Users.Remove(userToDelete);
                                    FilteredUsers.Refresh();
                                }
                                else
                                {
                                    MessageBox.Show(
                                        $"Failed to delete user: {responseData?.message ?? "Unknown error"}");
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error processing API response: {ex.Message}");
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
                }
            }
        }

        private void BranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BranchComboBox.SelectedValue != null)
            {
                int selectedBranchId = (int)BranchComboBox.SelectedValue;
                GetListUser(selectedBranchId); // Gọi lại với branch_id mới
            }
        }


        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            // Logic để xử lý khi nhấn nút Cancel
            ClearInputFields(); // Hàm để xóa dữ liệu trên các ô nhập
        }

        private void ClearInputFields()
        {
            // Xóa toàn bộ dữ liệu trong các trường input
            UserIDTextBox.Text = string.Empty;
            UserNameTextBox.Text = string.Empty;
            RateTextBox.Text = string.Empty;
            PasswordTextBox.Text = string.Empty;
            PermissionComboBox.SelectedIndex = -1;

            // Ẩn StackPanel chứa UserIDTextBox và thu gọn cột
            UserIDPanel.Visibility = Visibility.Collapsed;
            UserIDColumn.Width = new GridLength(0); // Thu gọn cột về 0

            // Reset trạng thái về thêm người dùng mới
            _selectedUser = null;
            SaveButton.Content = "Save";
        }

        private void ShowLoading(bool show)
        {
            Dispatcher.Invoke(() => { LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed; });
        }

        private FlowDocument CreateBillDocument()
        {
            string employeeId = UserIDTextBox.Text;
            string employee = UserNameTextBox.Text;
            string rate = RateTextBox.Text;

            FlowDocument document = new FlowDocument
            {
                PageWidth = 275,
                PagePadding = new Thickness(10),
                ColumnWidth = double.PositiveInfinity
            };

            // Add Image (SVG)


            // Title
            Paragraph title = new Paragraph(new Run("Bill For Employee"))
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
                    $"employeeId: {employeeId}\nEmployee: {employee}\nRate: {rate}\nFrom: {DateTime.Now:dd/MM/yyyy}\nTo: {DateTime.Now:dd/MM/yyyy}"))
                {
                    FontSize = 12,
                    TextAlignment = TextAlignment.Left,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(billInfo);
            string totalPrice = TotalPriceText.Text;
            string total_cash = TotalCashText.Text;
            string total_credit = TotalCreditText.Text;
            string total_profit = TotalProfitText.Text;
            string total_bill = TotalBillText.Text;


            // Total
            Paragraph totalParagraph =
                new Paragraph(new Run(
                    $"\n{totalPrice} $\n{total_cash} $\n{total_credit} $\n{total_profit} $\n{total_bill} $"))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Left,
                    FontFamily = new FontFamily("Roboto"),
                };
            document.Blocks.Add(totalParagraph);


            return document;
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


        private void PrintBill(FlowDocument document)
        {
            PrintDialog printDialog = new PrintDialog();

            // Lấy máy in mặc định và in tài liệu mà không cần hiển thị hộp thoại in
            var printQueue = printDialog.PrintQueue;
            XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(printQueue);
            writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);
        }

        private void PrintBillButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a FlowDocument for the bill
            FlowDocument billDocument = CreateBillDocument();

            // Show a preview of the bill
            // PreviewBill(billDocument);
            PrintBill(billDocument);
            // Optionally, you can also print the bill directly after previewing
            // Uncomment the following line if you want to print immediately after previewing
            // PrintBill(billDocument);
        }
    }
}