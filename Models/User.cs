using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternetShop.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } // Фамилия

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } // Имя

        [Required]
        [MaxLength(50)]
        public string Patronymic { get; set; } // Отчество

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } // Логин и email

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } // "Admin" или "User"

        public virtual ICollection<Order> Orders { get; set; }
    }
}
