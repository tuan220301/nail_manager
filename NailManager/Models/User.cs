using SQLite;

namespace NailManager.Models
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Permission { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public int BranchId { get; set; } 
    }

   
    public class UserFromListApi
    {
        public int user_id { get; set; }
        public string user_name { get; set; }
        public string name { get; set; }
        public string permission { get; set; }
        public string full_text_search { get; set; }
        public int branch_id { get; set; }
        public float rate { get; set; }
    }
}