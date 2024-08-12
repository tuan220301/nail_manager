using System.IO;
using NailManager.Models;
using SQLite;

namespace NailManager.Services
{
    public static class DatabaseHelper
    {
        private static readonly string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nailmanager.db");

        private static SQLiteAsyncConnection GetConnection()
        {
            return new SQLiteAsyncConnection(dbPath);
        }

        public static async Task InitializeDatabaseAsync()
        {
            var db = GetConnection();
            var tableInfo = await db.GetTableInfoAsync(nameof(User));

            if (!tableInfo.Any()) // Kiểm tra xem bảng User có tồn tại không
            {
                await db.CreateTableAsync<User>();
            }
        }

        public static async Task<User> GetUserAsync()
        {
            var db = GetConnection();
            return await db.Table<User>().FirstOrDefaultAsync();
        }

        public static async Task SaveUserAsync(User user)
        {
            var db = GetConnection();
            await db.InsertAsync(user);
        }

        public static async Task DeleteUserAsync(User user)
        {
            var db = GetConnection();
            await db.DeleteAsync(user);
        }
    }
}