using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using InternetShop.Data;
using InternetShop.Models;

namespace InternetShop.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ShopDbContext _context;

        public OrdersController(ShopDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string sortOrder, OrderStatus? status, DateTime? dateFrom, DateTime? dateTo)
        {
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.SelectedStatus = status;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.IdSortParm = sortOrder == "Id" ? "id_desc" : "Id";
            ViewBag.UserSortParm = sortOrder == "User" ? "user_desc" : "User";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.StatusSortParm = sortOrder == "Status" ? "status_desc" : "Status";
            ViewBag.CountSortParm = sortOrder == "Count" ? "count_desc" : "Count";
            ViewBag.SumSortParm = sortOrder == "Sum" ? "sum_desc" : "Sum";

            // Для фильтра
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Select(s => new { Id = (int)s, Name = s.ToString().Replace("_", " ") }), "Id", "Name", status);

            var orders = _context.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .AsQueryable();

            // Если пользователь не админ, показываем только его заказы
            if (!User.IsInRole("Admin"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID not found"));
                orders = orders.Where(o => o.UserId == userId);
            }

            // Для отображения названий товаров
            var productNames = _context.Products.ToDictionary(p => p.Id, p => p.Name);
            ViewBag.ProductNames = productNames;

            // Фильтрация по пользователю теперь происходит выше
            if (status.HasValue)
            {
                orders = orders.Where(o => o.Status == status.Value);
            }
            if (dateFrom.HasValue)
            {
                var from = dateFrom.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Utc)
                    : dateFrom.Value.ToUniversalTime();
                orders = orders.Where(o => o.OrderDate >= from);
            }
            if (dateTo.HasValue)
            {
                var to = dateTo.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTo.Value, DateTimeKind.Utc)
                    : dateTo.Value.ToUniversalTime();
                orders = orders.Where(o => o.OrderDate <= to);
            }

            switch (sortOrder)
            {
                case "Id":
                    orders = orders.OrderBy(o => o.Id);
                    break;
                case "id_desc":
                    orders = orders.OrderByDescending(o => o.Id);
                    break;
                case "User":
                    orders = orders.OrderBy(o => o.User.LastName);
                    break;
                case "user_desc":
                    orders = orders.OrderByDescending(o => o.User.LastName);
                    break;
                case "Date":
                    orders = orders.OrderBy(o => o.OrderDate);
                    break;
                case "date_desc":
                    orders = orders.OrderByDescending(o => o.OrderDate);
                    break;
                case "Status":
                    orders = orders.OrderBy(o => o.Status);
                    break;
                case "status_desc":
                    orders = orders.OrderByDescending(o => o.Status);
                    break;
                case "Count":
                    orders = orders.OrderBy(o => o.Items.Sum(i => i.Quantity));
                    break;
                case "count_desc":
                    orders = orders.OrderByDescending(o => o.Items.Sum(i => i.Quantity));
                    break;
                case "Sum":
                    orders = orders.OrderBy(o => o.Items.Sum(i => i.Price * i.Quantity));
                    break;
                case "sum_desc":
                    orders = orders.OrderByDescending(o => o.Items.Sum(i => i.Price * i.Quantity));
                    break;
                default:
                    orders = orders.OrderBy(o => o.Id);
                    break;
            }

            return View(await orders.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Проверяем, принадлежит ли заказ текущему пользователю или является ли пользователь админом
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!User.IsInRole("Admin") && userId != null && order.UserId != int.Parse(userId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(order);
        }

        // GET: Orders/Create
        [Authorize]
        public IActionResult Create()
        {
            var model = new OrderCreateViewModel
            {
                Products = _context.Products.Where(p => p.Stock > 0).Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock
                }).ToList()
            };
            return View(model);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            // Удаляем ошибки ModelState для технических полей, которые не должны валидироваться
            ModelState.Remove("Products");

            if (!ModelState.IsValid)
            {
                model.Products = _context.Products.Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock
                }).ToList();
                return View(model);
            }

            var order = new Order
            {
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Новый,
                Items = new List<OrderItem>(),
                UserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID not found"))
            };

            if (model.SelectedProducts != null)
            {
                foreach (var sel in model.SelectedProducts)
                {
                    if (sel.Quantity > 0)
                    {
                        var product = await _context.Products.FindAsync(sel.ProductId);
                        if (product != null)
                        {
                            order.Items.Add(new OrderItem
                            {
                                ProductId = product.Id,
                                Quantity = sel.Quantity,
                                Price = product.Price
                            });
                        }
                    }
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Уменьшаем количество товара на складе
            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock -= item.Quantity;
                    if (product.Stock < 0) product.Stock = 0;
                }
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Orders/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id, string sortOrder = null, OrderStatus? status = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewBag.CurrentSort = sortOrder;
            ViewBag.SelectedStatus = status;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,OrderDate,Status")] Order order, string sortOrder = null, OrderStatus? status = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            // Сохраняем параметры фильтрации в ViewBag
            ViewBag.CurrentSort = sortOrder;
            ViewBag.SelectedStatus = status;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _context.Orders
                        .Include(o => o.User)
                        .Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == id);

                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    existingOrder.Status = order.Status;
                    existingOrder.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                    _context.Entry(existingOrder).State = EntityState.Modified;

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { sortOrder, status, dateFrom, dateTo });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            // Загружаем данные заказа для отображения
            var orderToDisplay = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orderToDisplay == null)
            {
                return NotFound();
            }

            return View(orderToDisplay);
        }

        // GET: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
