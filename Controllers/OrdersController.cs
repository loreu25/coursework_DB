using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using InternetShop.Data;
using InternetShop.Models;

namespace InternetShop.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ShopDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ShopDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string sortOrder, int? customerId, OrderStatus? status, DateTime? dateFrom, DateTime? dateTo)
        {
            _logger.LogInformation("GET Index action called with parameters: sortOrder={sortOrder}, customerId={customerId}, status={status}, dateFrom={dateFrom}, dateTo={dateTo}",
                sortOrder, customerId, status, dateFrom, dateTo);

            try
            {
                ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
                ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
                ViewBag.SelectedCustomerId = customerId;
                ViewBag.SelectedStatus = status;
                ViewBag.CurrentSort = sortOrder;
                ViewBag.IdSortParm = sortOrder == "Id" ? "id_desc" : "Id";
                ViewBag.UserSortParm = sortOrder == "User" ? "user_desc" : "User";
                ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
                ViewBag.StatusSortParm = sortOrder == "Status" ? "status_desc" : "Status";
                ViewBag.CountSortParm = sortOrder == "Count" ? "count_desc" : "Count";
                ViewBag.SumSortParm = sortOrder == "Sum" ? "sum_desc" : "Sum";

                // Для фильтра
                ViewBag.Users = new SelectList(_context.Users.Where(u => u.Role == "User"), "Id", "LastName", customerId);
                ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Select(s => new { Id = (int)s, Name = s.ToString() }), "Id", "Name", status);

                var orders = _context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.User)
                    .AsQueryable();

                // Если пользователь не админ, показываем только его заказы
                if (!User.IsInRole("Admin"))
                {
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                    orders = orders.Where(o => o.UserId == userId);
                    _logger.LogInformation("Filtering orders for user {UserId}", userId);
                }

                // Для отображения названий товаров
                var productNames = _context.Products.ToDictionary(p => p.Id, p => p.Name);
                ViewBag.ProductNames = productNames;

                if (customerId.HasValue)
                {
                    orders = orders.Where(o => o.UserId == customerId.Value);
                    _logger.LogInformation("Filtering by user {UserId}", customerId.Value);
                }

                if (status.HasValue)
                {
                    orders = orders.Where(o => o.Status == status.Value);
                    _logger.LogInformation("Filtering by status {Status}", status.Value);
                }
                if (dateFrom.HasValue)
                {
                    var from = dateFrom.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Utc)
                        : dateFrom.Value.ToUniversalTime();
                    orders = orders.Where(o => o.OrderDate >= from);
                    _logger.LogInformation("Filtering by date from {DateFrom}", from);
                }
                if (dateTo.HasValue)
                {
                    var to = dateTo.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dateTo.Value, DateTimeKind.Utc)
                        : dateTo.Value.ToUniversalTime();
                    orders = orders.Where(o => o.OrderDate <= to);
                    _logger.LogInformation("Filtering by date to {DateTo}", to);
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
                        orders = orders.OrderByDescending(o => o.OrderDate);
                        break;
                }

                _logger.LogInformation("Successfully retrieved orders with sort order {SortOrder}", sortOrder);
                return View(await orders.ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                throw;
            }
        }

        // GET: Orders/Create
        [Authorize]
        public IActionResult Create()
        {
            _logger.LogInformation("GET Create action called");
            
            try
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

                _logger.LogInformation("Successfully prepared Create view model with {Count} available products", model.Products.Count);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing Create view model");
                throw;
            }
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            _logger.LogInformation("POST Create action called");
            
            ModelState.Remove("Products");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state in Create");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                    }
                }

                model.Products = _context.Products.Where(p => p.Stock > 0).Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock
                }).ToList();
                return View(model);
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var order = new Order
                {
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.New,
                    Items = new List<OrderItem>(),
                    UserId = userId
                };

                _logger.LogInformation("Creating new order for user {UserId}", userId);

                if (model.SelectedProducts != null)
                {
                    foreach (var sel in model.SelectedProducts)
                    {
                        if (sel.Quantity > 0)
                        {
                            var product = await _context.Products.FindAsync(sel.ProductId);
                            if (product != null)
                            {
                                if (product.Stock < sel.Quantity)
                                {
                                    _logger.LogWarning("Insufficient stock for product {ProductId}. Requested: {Requested}, Available: {Available}",
                                        product.Id, sel.Quantity, product.Stock);
                                    ModelState.AddModelError("", $"Недостаточно товара {product.Name} на складе");
                                    return View(model);
                                }

                                order.Items.Add(new OrderItem
                                {
                                    ProductId = product.Id,
                                    Quantity = sel.Quantity,
                                    Price = product.Price
                                });
                                _logger.LogInformation("Added product {ProductId} to order with quantity {Quantity}", product.Id, sel.Quantity);
                            }
                            else
                            {
                                _logger.LogWarning("Product {ProductId} not found", sel.ProductId);
                            }
                        }
                    }
                }

                if (!order.Items.Any())
                {
                    _logger.LogWarning("Attempted to create order with no items");
                    ModelState.AddModelError("", "Выберите хотя бы один товар");
                    return View(model);
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _context.Orders.Add(order);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Created order {OrderId}", order.Id);

                        foreach (var item in order.Items)
                        {
                            var product = await _context.Products.FindAsync(item.ProductId);
                            if (product != null)
                            {
                                product.Stock -= item.Quantity;
                                _logger.LogInformation("Updated stock for product {ProductId} to {Stock}", product.Id, product.Stock);
                            }
                        }
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        _logger.LogInformation("Successfully committed transaction for order {OrderId}", order.Id);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error during order creation transaction. Rolling back.");
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        // GET: Orders/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("GET Edit action called for order ID: {OrderId}", id);

            if (id == null)
            {
                _logger.LogWarning("Edit called with null ID");
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved order {OrderId} for editing", id);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,OrderDate,Status")] Order order)
        {
            _logger.LogInformation("POST Edit action called for order ID: {OrderId}", id);

            if (id != order.Id)
            {
                _logger.LogWarning("ID mismatch. Route ID: {RouteId}, Order ID: {OrderId}", id, order.Id);
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state in Edit");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                    }
                }
                
                // Загружаем User перед возвратом представления
                var orderWithUser = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(m => m.Id == id);
                
                if (orderWithUser != null)
                {
                    // Копируем свойства из переданной модели
                    orderWithUser.Status = order.Status;
                    orderWithUser.OrderDate = order.OrderDate;
                    
                    // Добавляем ошибки модели в ViewBag
                    ViewBag.ModelStateErrors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return View(orderWithUser);
                }
                
                return View(order);
            }

            try
            {
                var existingOrder = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (existingOrder == null)
                {
                    _logger.LogWarning("Order {OrderId} not found during update", id);
                    return NotFound();
                }

                // Проверяем, можно ли изменить статус
                if (!IsStatusChangeAllowed(existingOrder.Status, order.Status))
                {
                    _logger.LogWarning("Invalid status change from {OldStatus} to {NewStatus}", existingOrder.Status, order.Status);
                    ModelState.AddModelError("", $"Невозможно изменить статус с {existingOrder.Status} на {order.Status}");
                    
                    // Используем existingOrder вместо order, так как в нем уже загружен User
                    ViewBag.ModelStateErrors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return View(existingOrder);
                }

                existingOrder.Status = order.Status;
                existingOrder.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                _context.Entry(existingOrder).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated order {OrderId} to status {Status}", id, order.Status);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating order {OrderId}", id);

                if (!OrderExists(order.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", id);
                throw;
            }
        }

        // GET: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("GET Delete action called for order ID: {OrderId}", id);

            if (id == null)
            {
                _logger.LogWarning("Delete called with null ID");
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
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
            _logger.LogInformation("POST Delete action called for order ID: {OrderId}", id);

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order != null)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Восстанавливаем количество товаров на складе
                        foreach (var item in order.Items)
                        {
                            var product = await _context.Products.FindAsync(item.ProductId);
                            if (product != null)
                            {
                                product.Stock += item.Quantity;
                                _logger.LogInformation("Restored {Quantity} items to product {ProductId} stock", item.Quantity, item.ProductId);
                            }
                        }

                        _context.Orders.Remove(order);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        _logger.LogInformation("Successfully deleted order {OrderId}", id);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error during order deletion transaction. Rolling back.");
                        throw;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Order {OrderId} not found during delete", id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }

        private bool IsStatusChangeAllowed(OrderStatus currentStatus, OrderStatus newStatus)
        {
            switch (currentStatus)
            {
                case OrderStatus.New:
                    return newStatus == OrderStatus.Processing || newStatus == OrderStatus.Cancelled;
                case OrderStatus.Processing:
                    return newStatus == OrderStatus.Completed || newStatus == OrderStatus.Cancelled;
                case OrderStatus.Completed:
                case OrderStatus.Cancelled:
                    return false;
                default:
                    return false;
            }
        }
    }
}
