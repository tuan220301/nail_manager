namespace NailManager.Models;

public class ApiConnect
{
    public string Url { get; set; } = "https://api-nail.phungmup.online/api/v1";
    // public string Url { get; set; } = "https://172.16.1.45:3000/api/v1";
}
public class ResponseData
{
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
