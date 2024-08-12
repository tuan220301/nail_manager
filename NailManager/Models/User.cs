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
    }
}