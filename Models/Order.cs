using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace InternetShop.Models
{
    public enum OrderStatus
    {
        Новый = 0,
        В_обработке = 1,
        Завершён = 2,
        Отменён = 3
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }

        [ValidateNever]
        public List<OrderItem> Items { get; set; }

        [ValidateNever]
        public Customer Customer { get; set; }
    }
}
