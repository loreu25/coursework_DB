using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using InternetShop.Models;

namespace InternetShop.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ShopDbContext context)
        {
            context.Database.EnsureCreated();

            // Проверяем, есть ли уже администратор
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                // Создаем администратора
                var admin = new User
                {
                    LastName = "Admin",
                    FirstName = "Admin",
                    Patronymic = "Admin",
                    Email = "admin@shop.com",
                    PasswordHash = ComputeSha256Hash("admin"), // Пароль: admin
                    Role = "Admin"
                };

                context.Users.Add(admin);
                context.SaveChanges();
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
