using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InternetShop.Data;
using InternetShop.Models;

namespace InternetShop.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ShopDbContext _context;

        public OrdersController(ShopDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string sortOrder, int? customerId, OrderStatus? status, DateTime? dateFrom, DateTime? dateTo)
        {
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCustomerId = customerId;
            ViewBag.SelectedStatus = status;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.IdSortParm = sortOrder == "Id" ? "id_desc" : "Id";
            ViewBag.ClientSortParm = sortOrder == "Client" ? "client_desc" : "Client";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.StatusSortParm = sortOrder == "Status" ? "status_desc" : "Status";
            ViewBag.CountSortParm = sortOrder == "Count" ? "count_desc" : "Count";
            ViewBag.SumSortParm = sortOrder == "Sum" ? "sum_desc" : "Sum";

            // Для фильтра
            ViewBag.Customers = new SelectList(_context.Customers.OrderBy(c => c.Name).ToList(), "Id", "Name", customerId);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Select(s => new { Id = (int)s, Name = s.ToString().Replace("_", " ") }), "Id", "Name", status);

            var orders = _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Customer)
                .AsQueryable();

            // Для отображения названий товаров
            var productNames = _context.Products.ToDictionary(p => p.Id, p => p.Name);
            ViewBag.ProductNames = productNames;

            if (customerId.HasValue)
            {
                orders = orders.Where(o => o.CustomerId == customerId.Value);
            }
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
                case "Client":
                    orders = orders.OrderBy(o => o.Customer.Name);
                    break;
                case "client_desc":
                    orders = orders.OrderByDescending(o => o.Customer.Name);
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
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            var customers = _context.Customers.ToList();
            if (!customers.Any())
            {
                TempData["ErrorMessage"] = "Сначала создайте хотя бы одного клиента.";
                return RedirectToAction("Index", "Customers");
            }
            var products = _context.Products.ToList();
            var statuses = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = s.ToString().Replace("_", " ")
            }).ToList();
            var model = new OrderCreateViewModel
            {
                Customers = customers.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList(),
                Products = products.Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock
                }).ToList(),
                Statuses = statuses,
                OrderDate = DateTime.Now
            };
            return View(model);
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            // Удаляем ошибки ModelState для технических полей, которые не должны валидироваться
            ModelState.Remove("Products");
            ModelState.Remove("Statuses");
            ModelState.Remove("Customers");

            if (!ModelState.IsValid)
            {
                model.Customers = _context.Customers.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
                model.Products = _context.Products.Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock
                }).ToList();
                model.Statuses = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.ToString().Replace("_", " ")
                }).ToList();
                return View(model);
            }

            var order = new Order
            {
                CustomerId = model.CustomerId,
                OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc),
                Status = model.Status,
                Items = new List<OrderItem>()
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,OrderDate,Status")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            // Добавляем ошибки ModelState во ViewBag для диагностики
            ViewBag.ModelStateErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
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
