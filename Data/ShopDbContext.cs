using Microsoft.EntityFrameworkCore;
using InternetShop.Models;

namespace InternetShop.Data
{
    public class ShopDbContext : DbContext
    {
        public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options) { }


        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed admin user
            var adminPassword = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes("admin")));
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                LastName = "Админ",
                FirstName = "Системы",
                Patronymic = "Тестович",
                Email = "admin@site.ru",
                PasswordHash = adminPassword,
                Role = "Admin"
            });
        }
    }
}
