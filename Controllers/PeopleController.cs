using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using System.Security.Claims;
using System.Text.Json;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class PeopleController : BaseController
    {
        private readonly ILogger<PeopleController> _logger;
        private readonly RestaurantDBContext _dbContext;

        public PeopleController(ILogger<PeopleController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
        : base(languageService, localizationService, dateTimeService)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /*
         * Customer Views
         */

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> Customers()
        {
            var users = await _dbContext.Customers.ToListAsync();
            return View(users);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult AddCustomer()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult EditCustomer(int? Id)
        {
            var existing = _dbContext.Customers.FirstOrDefault(x => x.Id == Id);
            if (existing != null)
            {
                return View(existing);
            }
            return NotFound();
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult ViewCustomer(int? Id)
        {
            var existing = _dbContext.Customers.FirstOrDefault(x => x.Id == Id);
            if (existing != null)
            {
                var orders = _dbContext.Orders.Where(x => x.CustomerId == existing.Id);
                var totalSales = orders.Sum(x => x.Total);
                var paidAmount = orders.Sum(x => x.PaidAmount);
                ViewBag.totalSales = Math.Round(totalSales, 2);
                ViewBag.totalDue = Math.Round(totalSales - paidAmount, 2);
                ViewBag.totalOrders = orders.Count();
                
                return View(existing);
            }
            return NotFound();
        }

        /*
         * Customer APIs
         */

        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                customer.UpdatedAt = CurrentDateTime();
                customer.UpdatedBy = userName;
                customer.Email = customer.Email?.ToLower();

                if (!string.IsNullOrEmpty(customer.Phone))
                {
                    var existingPhone = await _dbContext.Customers.AnyAsync(x => x.Phone == customer.Phone);
                    if (existingPhone)
                    {
                        response.Add("status", "error");
                        response.Add("message", "This phone number is already registered.");
                        return Json(response);
                    }
                }

                try
                {
                    _dbContext.Customers.Add(customer);
                    await _dbContext.SaveChangesAsync();

                    response.Add("customer", JsonSerializer.Serialize(customer));
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving customer.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Enter required fields.");
            }
            return Json(response);
        }

        [HttpPut]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateCustomer(Customer customer)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                customer.UpdatedAt = CurrentDateTime();
                customer.UpdatedBy = userName;
                customer.Email = customer.Email?.ToLower();

                if (!string.IsNullOrEmpty(customer.Phone))
                {
                    var existingPhone = await _dbContext.Customers.AnyAsync(x => x.Phone == customer.Phone && x.Id != customer.Id);
                    if (existingPhone)
                    {
                        response.Add("status", "error");
                        response.Add("message", "This phone number is already registered to another customer.");
                        return Json(response);
                    }
                }

                try
                {
                    _dbContext.Customers.Update(customer);
                    await _dbContext.SaveChangesAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while updating customer.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Enter required fields.");
            }
            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin,staff")]
        public async Task<JsonResult> DeleteCustomer(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Customers.FindAsync(Id);
            
            if (existing != null)
            {
                try
                {
                    if (GetSetting.DefaultCustomer == existing.Id)
                    {
                        response.Add("status", "error");
                        response.Add("message", "You can't delete default customer.");
                    }
                    else
                    {
                        _dbContext.Customers.Remove(existing);
                        _dbContext.SaveChanges();

                        response.Add("status", "success");
                        response.Add("message", "success");
                    }
                }
                catch (DbUpdateException)
                {
                    response.Add("status", "error");
                    response.Add("message", "Your attempt to delete record could not be completed because it is associated with other table.");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while deleting customer.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Customer not exist.");
            }
            
            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> SearchCustomer(string search)
        {
            var response = new Dictionary<string, string>();
            try
            {
                IQueryable<Customer> query = _dbContext.Customers;
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(x => x.CustomerName.Contains(search) || x.Phone.Contains(search) || x.Id == GetSetting.DefaultCustomer);
                }

                var customers = await query.OrderByDescending(x => x.Id).ToListAsync();

                string obj = JsonSerializer.Serialize(customers);
                response.Add("data", obj);
                response.Add("status", "success");
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        /*
         * Supplier Views
        */

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> Suppliers()
        {
            var users = await _dbContext.Suppliers.ToListAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult AddSupplier()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult EditSupplier(int? Id)
        {
            var existing = _dbContext.Suppliers.FirstOrDefault(x => x.Id == Id);
            if (existing != null)
            {
                return View(existing);
            }
            return NotFound();
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult ViewSupplier(int? Id)
        {
            var existing = _dbContext.Suppliers.FirstOrDefault(x => x.Id == Id);
            if (existing != null)
            {
                var purchases = _dbContext.Purchases.Where(x => x.SupplierId == existing.Id);
                var totalPurchases = purchases.Sum(x => x.TotalAmount);
                var dueAmount = purchases.Sum(x => x.DueAmount);
                ViewBag.totalPurchases = Math.Round(totalPurchases, 2);
                ViewBag.totalDue = Math.Round(dueAmount, 2);
                ViewBag.totalOrders = purchases.Count();

                return View(existing);
            }
            return NotFound();
        }

        /*
         * Supplier APIs
        */
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> AddSupplier(Supplier supplier)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                supplier.UpdatedAt = CurrentDateTime();
                supplier.UpdatedBy = userName;
                supplier.Email = supplier.Email.ToLower();

                try
                {
                    _dbContext.Suppliers.Add(supplier);
                    await _dbContext.SaveChangesAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving supplier.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }
            
            return Json(response);
        }

        [HttpPut]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateSupplier(Supplier supplier)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                supplier.UpdatedAt = CurrentDateTime();
                supplier.UpdatedBy = userName;
                supplier.Email = supplier.Email.ToLower();

                try
                {
                    _dbContext.Suppliers.Update(supplier);
                    await _dbContext.SaveChangesAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while updating supplier.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }
            
            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin,staff")]
        public async Task<JsonResult> DeleteSupplier(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Suppliers.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    _dbContext.Suppliers.Remove(existing);
                    await _dbContext.SaveChangesAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch (DbUpdateException)
                {
                    response.Add("status", "error");
                    response.Add("message", "Your attempt to delete record could not be completed because it is associated with other table.");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while deleting supplier.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Supplier not exist.");
            }
            
            return Json(response);
        }
    }
}