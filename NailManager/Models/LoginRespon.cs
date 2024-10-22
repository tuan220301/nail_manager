namespace NailManager.Models;

public class LoginRespon
{
    public int status { get; set; }
    public string message { get; set; }
    public DataRespon data { get; set; }
}

public class DataRespon
{
    public string user_name { get; set; }
    public string name { get; set; }
    public DateTime create_at { get; set; }
    public DateTime update_at { get; set; }
    public int user_id { get; set; }
    public string permision { get; set; }
    public string access_token { get; set; }
    public int branch_id { get; set; }
}