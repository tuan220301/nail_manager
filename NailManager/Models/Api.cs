namespace NailManager.Models;

public class ApiConnect
{
    public string Url { get; set; } = "https://api-nail.phungmup.online/api/v1";
    // public string Url { get; set; } = "http://localhost:8000/api/v1";
}
public class ResponseData
{
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
