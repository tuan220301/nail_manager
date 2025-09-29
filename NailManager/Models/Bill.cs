using System.ComponentModel;

namespace NailManager.Models
{
   
    public class BilLResponBranch
    {
        public int? bill_id { get; set; }
        public string customer_name { get; set; }
        public string customer_phone { get; set; }
        public int status { get; set; }
        public double total_price { get; set; }
        public DateTime created_at { get; set; }
        public List<ProductInBillModel> service { get; set; } // Đổi tên thuộc tính thành 'list' để phù hợp với API response
    }

    public class ProductInBillModel
    {
        public string product_name { get; set; }
        public string user_name { get; set; } // is created by client for show name of employee on screen
        public int bill_detail_id { get; set; }
        public int bill_id { get; set; }
        public double price { get; set; }
        public double service_fee { get; set; }
        public int user_id { get; set; }
        
    }

    public class BillListRespon
    {
        public int status { get; set; }
        public string message { get; set; }
        public BillList data { get; set; } // Đổi tên từ 'billList' thành 'data' để phù hợp với API response
    }
    public class BillList
    {
        public List<Bill> list { get; set; } // Đổi tên thuộc tính thành 'list' để phù hợp với API response
        public double total_price { get; set; }
        public double total_bill { get; set; }
        public double total_profit { get; set; }
    }

    public class Bill
    {
        public int bill_id { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string customer_phone { get; set; } = string.Empty;
        public int branch_id { get; set; } 
        public string status { get; set; } 
        public double price { get; set; } 
        public DateTime created_at { get; set; }
    }

   

    public class ListService
    {
        public int user_id { get; set; }
        public string? user_name { get; set; }
        public double other_price { get; set; }
        public double service_fee { get; set; }
        public List<ListProductInBill> product { get; set; }
    }

    public class ListProductInBill
    {
        public int product_id { get; set; }
        public double discount { get; set; }
    }

    public class ListBillModel
    {
        public int status { get; set; }
        public string message { get; set; }
        public ListBill data { get; set; }
    }

    public class ListBill
    {
        public List<ListBillRespone> list { get; set; }
        public double total_price { get; set; }
        public double total_profit { get; set; }
        public double total_bill { get; set; }
        public double total_service_fee { get; set; }
        
    }
    public class ListBillRespone
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public int bill_id { get; set; }
        public int branch_id { get; set; }
        public string customer_name { get; set; }
        public string customer_phone { get; set; }
        public string status { get; set; }
        public double total_price { get; set; }
        public double price { get; set; }
        public double service_fee { get; set; }
        public DateTime created_at { get; set; }
    }
}
