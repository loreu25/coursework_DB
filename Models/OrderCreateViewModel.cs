using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace InternetShop.Models
{
    public class OrderCreateViewModel
    {
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        [BindNever]
        public List<SelectListItem> Statuses { get; set; }

        [BindNever]
        public List<SelectListItem> Customers { get; set; }

        [BindNever]
        public List<ProductViewModel> Products { get; set; }

        public List<SelectedProduct> SelectedProducts { get; set; } = new List<SelectedProduct>();
    }

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    public class SelectedProduct
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
