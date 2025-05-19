using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace InternetShop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно для заполнения")]
        [StringLength(100, ErrorMessage = "Название не может быть длиннее 100 символов")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Описание не может быть длиннее 500 символов")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна для заполнения")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Количество на складе обязательно для заполнения")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество на складе не может быть отрицательным")]
        public int Stock { get; set; }

        [ValidateNever]
        public List<OrderItem> OrderItems { get; set; }
    }
}
