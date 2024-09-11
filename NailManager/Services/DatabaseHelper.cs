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

            // Kiểm tra xem bảng User đã tồn tại hay chưa
            var tableInfo = await db.GetTableInfoAsync(nameof(User));

            if (!tableInfo.Any()) // Nếu bảng không tồn tại, tạo bảng mới
            {
                await db.CreateTableAsync<User>();
            }
            else
            {
                // Nếu bảng đã tồn tại, kiểm tra xem có cột nào bị thiếu không
                var missingColumns = typeof(User).GetProperties()
                    .Select(p => p.Name)
                    .Except(tableInfo.Select(t => t.Name));

                if (missingColumns.Any())
                {
                    // Xóa bảng cũ và tạo lại bảng mới để thêm các cột bị thiếu
                    await db.DropTableAsync<User>();
                    await db.CreateTableAsync<User>();
                }
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
            try
            {
                var db = GetConnection();
                await db.DeleteAsync(user);
                Console.WriteLine("User deleted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
            }
        }

    }
}