using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace InternetShop.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна для заполнения")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилия должна быть от 2 до 50 символов")]
        [RegularExpression(@"^[A-Za-zА-Яа-я\-]+$", ErrorMessage = "Фамилия может содержать только буквы и дефис")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Имя обязательно для заполнения")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 50 символов")]
        [RegularExpression(@"^[A-Za-zА-Яа-я\-]+$", ErrorMessage = "Имя может содержать только буквы и дефис")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Отчество обязательно для заполнения")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Отчество должно быть от 2 до 50 символов")]
        [RegularExpression(@"^[A-Za-zА-Яа-я\-]+$", ErrorMessage = "Отчество может содержать только буквы и дефис")]
        public string Patronymic { get; set; }

        [Required(ErrorMessage = "Email обязателен для заполнения")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [StringLength(100, ErrorMessage = "Email не может быть длиннее 100 символов")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен для заполнения")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Роль обязательна для заполнения")]
        [StringLength(20, ErrorMessage = "Роль не может быть длиннее 20 символов")]
        [RegularExpression("^(Admin|User)$", ErrorMessage = "Роль может быть только 'Admin' или 'User'")]
        public string Role { get; set; }

        [ValidateNever]
        public virtual ICollection<Order> Orders { get; set; }
    }
}
