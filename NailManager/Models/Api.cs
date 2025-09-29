namespace NailManager.Models;

public class ApiConnect
{
    //product
    public string Url { get; set; } = "https://hainam.logit.id.vn/api-nail/v1";
    
    //test
    // public string Url { get; set; } = "https://beta-nail.phungmup.online/api/v1";
}
public class ResponseData
{
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
public class ApiResponse
{
    public int status { get; set; }
    public string message { get; set; }
    public object data { get; set; }
}

public class APIResponFromCancelBill
{
    public int status { get; set; }
    public string message { get; set; }
}