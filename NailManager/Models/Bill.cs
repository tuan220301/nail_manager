namespace NailManager.Models
{
    public class Bill
    {
        public int bill_id { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string customer_phone { get; set; } = string.Empty;
        public string user_name { get; set; } = string.Empty;
        public int user_id { get; set; } 
        public int branch_id { get; set; } 
        public int pay_method { get; set; } 
        public int discount { get; set; } 
        public string name { get; set; } = string.Empty;
        public int status { get; set; } 
        public int total_price { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public List<Product> products { get; set; }
    }

    public class BillList
    {
        public List<Bill> list { get; set; } // Đổi tên thuộc tính thành 'list' để phù hợp với API response
        public double total_price { get; set; }
        public double total_cash { get; set; }
        public double total_credit { get; set; }
    }

    public class BillListRespon
    {
        public int status { get; set; }
        public string message { get; set; }
        public BillList data { get; set; } // Đổi tên từ 'billList' thành 'data' để phù hợp với API response
    }

    public class BillFromList
    {
        public int bill_id { get; set; }
        public string customer_name { get; set; }
        public string customer_phone { get; set; }
        public int branch_id { get; set; }
        public int pay_method { get; set; }
        public int user_id { get; set; }
        public double total_price { get; set; }
        public int discount { get; set; }
        public int status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class BillDetail
    {
        public int bill_id { get; set; }
        public string customer_name { get; set; }
        public string customer_phone { get; set; }
        public int user_id { get; set; }
        public string user_name { get; set; }
        public string name { get; set; }
        public int status { get; set; }
        public int pay_method { get; set; }
        public double total_price { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public List<ProductInBill> products { get; set; }
    }

    public class ProductInBill
    {
        public int bill_detail_id { get; set; }
        public int bill_id { get; set; }
        public int product_id { get; set; }
        public string product_name { get; set; }
        public double price { get; set; }
        public bool IsNewlyAdded { get; set; } = false; // Mặc định là false
    }
    public class BillListResponse
    {
        public List<Bill> list { get; set; }
        public double total_price { get; set; }
        public double total_cash { get; set; }
        public double total_credit { get; set; }
        public double total_profit { get; set; }
        public int total_bill { get; set; }
    }
}
