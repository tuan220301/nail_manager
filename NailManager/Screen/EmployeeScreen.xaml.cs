using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Printing;
using System.Text;
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


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Biến để lưu trữ người dùng đang được chỉnh sửa
        private UserFromListApi _selectedUser;
        private User userLocal { get; set; }

        public EmployeeScreen()
        {
            InitializeComponent();

            // Khởi tạo Users và FilteredUsers
            Users = new ObservableCollection<UserFromListApi>();
            FilteredUsers = CollectionViewSource.GetDefaultView(Users);

            // Đặt DataContext
            DataContext = this;

            // Thiết lập ngày mặc định cho DatePicker
            // DatePickerInputTo.SelectedDate = DateTime.Today;
            // DatePickerInputFrom.SelectedDate = DateTime.Today;

            // Lấy thông tin người dùng và thiết lập giao diện
            GetUser();
            SaveButton.Visibility = Visibility.Collapsed;
            // Lấy danh sách chi nhánh
            // GetBrandList();
            // Lấy danh sách người dùng
            // GetListUser();
        }

        private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kiểm tra xem người dùng đã được chọn hay chưa
                if (_selectedUser == null)
                {
                    MessageBox.Show("Please select a user to update.", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Chuẩn bị tham số cho API
                var parameters = new Dictionary<string, string>
                {
                    { "user_id", _selectedUser.user_id.ToString() },
                    { "name", UserNameTextBox.Text }
                };

                // URL API
                string apiUrl = "/user/update";

                // Gọi hàm PostApiAsync để gửi yêu cầu
                var apiService = new Api();
                await apiService.PostApiAsync(apiUrl, parameters, (responseBody) =>
                {
                    try
                    {
                        // Parse response để kiểm tra xem có thành công không
                        var responseData = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
                        string responseDataJson = JsonConvert.SerializeObject(responseBody, Formatting.Indented);
                        Console.WriteLine("responseData update user: ");
                        Console.WriteLine(responseDataJson);
                        if (responseData != null && responseData.status == 200)
                        {
                            MessageBox.Show("User name updated successfully!", "Success", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            GetListUser(userLocal.BranchId);
                        }
                        else
                        {
                            MessageBox.Show($"Failed to update user name: {responseData?.message ?? "Unknown error"}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(ex.Message);
            }
        }


        private async void OnUserItemClicked(object sender, MouseButtonEventArgs e)
        {
            string password = null;
            bool? dialogResult = null;
            string permissionType = null;
            AuthenticationWindow authWindow = new AuthenticationWindow();
            UserFromListApi selectedUser = null;

            // Lấy dữ liệu user từ sự kiện click nếu có
            if (sender is Border border && border.DataContext is UserFromListApi clickedUser)
            {
                selectedUser = clickedUser;
                _selectedUser = selectedUser;
            }

            if (permision != "1")
            {
                dialogResult = authWindow.ShowDialog();
                permissionType = authWindow.UserPermission;

                if (dialogResult != true || !authWindow.IsAuthenticated)
                {
                    return; // Không xác thực được, thoát
                }

                password = authWindow.EnteredPassword;
            }

            ShowLoading(true);

            // Kiểm tra loại permission để xác định logic branch hay user
            if (permissionType == "Branch")
            {
                // Gọi API xử lý cho Branch với thông tin username và password
                var username = authWindow.EnteredUsername;
                await SubmitBranchLogin(username, password);
                SaveButton.Visibility = Visibility.Visible;
                UserNameTextBox.Text = selectedUser?.name;
                RateTextBox.Text = selectedUser?.rate.ToString();
                UserIDTextBox.Text = selectedUser?.user_id.ToString();

                // Truyền user_id từ selectedUser nếu có
                await GetUserDataAndBills(isBranch: true, userId: selectedUser?.user_id, password: password);
            }
            else
            {
                if (selectedUser != null)
                {
                    // Nếu là user, ẩn nút Save và gán các giá trị từ dòng được chọn vào các TextBox
                    SaveButton.Visibility = Visibility.Collapsed;
                    UserIDTextBox.Text = selectedUser.user_id.ToString();
                    UserNameTextBox.Text = selectedUser.name;
                    RateTextBox.Text = selectedUser.rate.ToString();
                    PasswordTextBox.Text = ""; // Không hiển thị mật khẩu thực
                    UserIDPanel.Visibility = Visibility.Visible;
                    UserIDColumn.Width = new GridLength(200);

                    // Gọi hàm lấy thông tin dữ liệu
                    await GetUserDataAndBills(isBranch: false, userId: selectedUser.user_id, password: password);
                }
            }

            ShowLoading(false);
        }

// Hàm chung để lấy dữ liệu và gọi API bills
        private async Task GetUserDataAndBills(bool isBranch, int? userId = null, string password = null)
        {
            FilteredBills.Clear();
            // Thiết lập thời gian từ và đến mặc định
            DateTime fromDate = DateTime.Today;
            DateTime toDate = DateTime.Today;
            // DateTime adjustedFromDate = fromDate.Add(-offset);
            // DateTime adjustedToDate = toDate.Add(-offset).AddHours(23).AddMinutes(59).AddSeconds(59);

            string startDay = fromDate.ToString("yyyy-MM-dd HH:mm:ss");
            string endDay = toDate.ToString("yyyy-MM-dd HH:mm:ss");

            // Kiểm tra branch_id
            int branchId;
            if (userLocal?.BranchId != null)
            {
                branchId = userLocal.BranchId;
            }
            else if (BranchId > 0)
            {
                branchId = BranchId;
            }
            else
            {
                Console.WriteLine("Error: branch_id is missing or invalid.");
                MessageBox.Show("Branch ID is missing or invalid.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Thiết lập URL API và tham số tùy thuộc vào loại user
            string apiUrl = isBranch ? "/bill/branch/get" : "/bill/user/get";

            var parameters = new Dictionary<string, string>
            {
                { "branch_id", branchId.ToString() },
                { "start_day", startDay },
                { "end_day", endDay }
            };

            // Chỉ thêm user_id nếu nó có giá trị hợp lệ
            if (userId.HasValue && userId.Value > 0)
            {
                parameters["user_id"] = userId.Value.ToString();
            }

            if (!string.IsNullOrEmpty(password) && !isBranch)
            {
                parameters["password"] = password;
            }

            if (isBranch)
            {
                parameters["status"] = "-1";
            }

            Console.WriteLine("Parameters for API request:");
            foreach (var param in parameters)
            {
                Console.WriteLine($"{param.Key}: {param.Value}");
            }

            var apiService = new Api();
            
            await apiService.PostApiAsync(apiUrl, parameters, (responseBody) =>
            {
                try
                {
                    var responseData = JsonConvert.DeserializeObject<ListBillModel>(responseBody);
                    string responseDataJson = JsonConvert.SerializeObject(responseBody, Formatting.Indented);
                    Console.WriteLine("responseData from employee: ");
                    Console.WriteLine(responseDataJson);
                    if (responseData != null && responseData.status == 200)
                    {
                        // Xóa dữ liệu cũ và cập nhật dữ liệu mới
                        
                        TotalPriceText.Text = $"Total Price: {responseData.data.total_price}";
                        TotalProfitText.Text = $"Total Profit: {responseData.data.total_profit}";
                        TotalBillText.Text = $"Total Bill: {responseData.data.total_bill}";

                        foreach (var bill in responseData.data.list)
                        {
                            FilteredBills.Add(new Bill
                            {
                                bill_id = bill.bill_id,
                                customer_name = bill.customer_name,
                                customer_phone = bill.customer_phone,
                                branch_id = bill.branch_id,
                                price = bill.total_price,
                                created_at = bill.created_at,
                                status = FormatStatus(bill.status)
                            });
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
                    MessageBox.Show($"Error parsing response: {ex.Message}");
                }
            });
        }


        private string FormatStatus(string status)
        {
            return status == "1" ? "Success" : "Cancel";
        }

        private async void GetUser()
        {
            ShowLoading(true);
            try
            {
                var user = await DatabaseHelper.GetUserAsync();
                if (user != null)
                {
                    GetListUser(user.BranchId);
                    userLocal = user;
                    // if (user.BranchId > 0)
                    // {
                    //     BranchId = user.BranchId;
                    //     BranchComboBox.Visibility = Visibility.Collapsed;
                    //     BorderBranch.Visibility = Visibility.Collapsed;
                    // }
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

        private async Task<bool> SubmitBranchLogin(string username, string password)
        {
            try
            {
                string json = JsonConvert.SerializeObject(new { user_name = username, password = password });
                ApiConnect apiString = new ApiConnect();
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"{apiString.Url}/auth/login", content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        LoginRespon? responseData = JsonConvert.DeserializeObject<LoginRespon>(responseBody);
                        string responseDataJson = JsonConvert.SerializeObject(responseBody, Formatting.Indented);
                        Console.WriteLine("responseData (JSON) in login from employee: ");
                        Console.WriteLine(responseDataJson);
                        if (responseData?.data != null)
                        {
                            return true; // Đăng nhập thành công
                        }
                    }
                }

                return false; // Đăng nhập thất bại
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during branch login: {ex.Message}");
                return false;
            }
        }


        private async void GetListUser(int branchId)
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

            // Ẩn StackPanel chứa UserIDTextBox và thu gọn cột
            UserIDPanel.Visibility = Visibility.Collapsed;
            UserIDColumn.Width = new GridLength(0); // Thu gọn cột về 0

            // Reset trạng thái về thêm người dùng mới
            _selectedUser = null;
        }

        private void ShowLoading(bool show)
        {
            Dispatcher.Invoke(() => { LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed; });
        }

        private FlowDocument CreateBillDocument()
        {
            if (FilteredBills.Count() == 0)
            {
                MessageBox.Show(
                    $"Please waiting for the bill to be generated. Please try again after some time.");
                return null;
            }
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
            string total_profit = TotalProfitText.Text;
            string total_bill = TotalBillText.Text;


            // Total
            Paragraph totalParagraph =
                new Paragraph(new Run(
                    $"\n{totalPrice} $\n{total_profit} $\n{total_bill}"))
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
            if (FilteredBills.Count > 0)
            {
                PrintBill(billDocument);
            }
            else
            {
                MessageBox.Show($"Cannot print Bill with empty list", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}