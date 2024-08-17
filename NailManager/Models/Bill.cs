namespace NailManager.Models;

public class Bill
{
   public int bill_id { get; set; }
   public string customer_name { get; set; } = string.Empty;
   public string customer_phone { get; set; } = string.Empty;
   public string user_name { get; set; }= string.Empty;
   public int user_id { get; set; } 
   public string name { get; set; } = string.Empty;
   public int status { get; set; } 
   public int total_price { get; set; }
   public DateTime created_at { get; set; }
   public DateTime updated_at { get; set; }
public  List<Product> products { get; set; }
}

public class BillList
{
   public List<Bill> bills { get; set; }
}
public class BillListRespon 
{
   public int status { get; set; }
   public string message { get; set; }
   public BillList billList { get; set; }
}

public class BillFromList
{
   public int bill_id { get; set; }
   public string customer_name { get; set; }
   public string customer_phone { get; set; }
   public int branch_id { get; set; }
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
   public double total_price { get; set; }
   public DateTime created_at { get; set; }
   public DateTime updated_at { get; set; }
   public List<ProductInBill> products { get; set; }
}

public class ProductInBill
{
   public int bill_detail_id { get; set; }
   public int bill_id { get; set; }
   public int quantity { get; set; }
   public int product_id { get; set; }
   public string name { get; set; }
   public double price { get; set; }
   public int Quantity { get; set; } // Để ánh xạ với SelectedItems
   public bool IsNewlyAdded { get; set; } = false; // Mặc định là false
}
