namespace NailManager.Models;

public class ApiConnect
{
    public string Url { get; set; } = "http://192.168.1.248:3000/api/v1";
}
public class ResponseData
{
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
