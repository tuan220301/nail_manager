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