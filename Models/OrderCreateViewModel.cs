using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace InternetShop.Models
{
    public class OrderCreateViewModel
    {
        [ValidateNever]
        public List<ProductViewModel> Products { get; set; }

        [Required(ErrorMessage = "Выберите хотя бы один товар")]
        public List<SelectedProduct> SelectedProducts { get; set; } = new List<SelectedProduct>();
    }

    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Количество на складе не может быть отрицательным")]
        public int Stock { get; set; }
    }

    public class SelectedProduct
    {
        [Required]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Укажите количество")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
        public int Quantity { get; set; }
    }
}
