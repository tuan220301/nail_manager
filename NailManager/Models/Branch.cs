namespace NailManager.Models;

public class Branch
{
    public int branch_id { get; set; }
    public string name { get; set; }
    public int status { get; set; }
    public string address { get; set; }
    public string open_close { get; set; }
    public string sdt { get; set; }
    public string website { get; set; }
    public string description { get; set; }
}

// Define the API response class
public class BranchApiResponse<T>
{
    public int status { get; set; }
    public string message { get; set; }
    public T data { get; set; }
}
public class CreateProductResponse
{
    public int product_id { get; set; }
}
public class UserResponseData
{
    public int user_id { get; set; }
}
public class BillResponseData
{
    public int bill_id { get; set; }
}

// Định nghĩa lớp API response chung
