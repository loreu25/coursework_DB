using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace InternetShop.Models
{
    public enum OrderStatus
    {
        New = 0,
        Processing = 1,
        Completed = 2,
        Cancelled = 3
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        [ValidateNever]
        public List<OrderItem> Items { get; set; }

        [ValidateNever]
        public User User { get; set; }
    }
}
