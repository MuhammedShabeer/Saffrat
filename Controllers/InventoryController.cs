using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Saffrat.Controllers
{
    [Authorize(Roles = "admin,staff")]
    public class InventoryController : BaseController
    {
        private readonly RestaurantDBContext _dbContext;

        public InventoryController(RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
            : base(languageService, localizationService, dateTimeService)
        {
            _dbContext = dbContext;
        }

        // List all van stocks
        public async Task<IActionResult> Index()
        {
            var stocks = await _dbContext.FoodItemStocks
                .Include(s => s.FoodItem)
                .OrderBy(s => s.UserId)
                .ToListAsync();

            // Removed driver logic as stock is now globally tracked for Van Sale
            ViewBag.FoodItems = await _dbContext.FoodItems
                .Where(x => x.PermittedSalesTypes != null && x.PermittedSalesTypes.Contains("VanSale"))
                .ToListAsync();

            return View(stocks);
        }

        // Load Stock to a Van
        [HttpPost]
        public async Task<IActionResult> LoadStock(string UserId, int FoodItemId, decimal Quantity, string Type = "Load")
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Globally track all van stock without tying it to a specific driver
            UserId = "VanStock";

            try
            {
                var stock = await _dbContext.FoodItemStocks.FirstOrDefaultAsync(x => x.UserId == UserId && x.FoodItemId == FoodItemId);
                if (stock == null)
                {
                    stock = new FoodItemStock
                    {
                        UserId = UserId,
                        FoodItemId = FoodItemId,
                        Quantity = Quantity,
                        UpdatedAt = CurrentDateTime()
                    };
                    _dbContext.FoodItemStocks.Add(stock);
                }
                else
                {
                    if (Type == "Load") stock.Quantity += Quantity;
                    else if (Type == "Return") stock.Quantity -= Quantity;
                    else stock.Quantity = Quantity; // Set

                    stock.UpdatedAt = CurrentDateTime();
                    _dbContext.FoodItemStocks.Update(stock);
                }

                // Log Transaction
                _dbContext.InventoryTransactions.Add(new InventoryTransaction
                {
                    UserId = UserId,
                    FoodItemId = FoodItemId,
                    QuantityChange = Type == "Return" ? -Quantity : Quantity,
                    Type = Type,
                    EntryDate = CurrentDateTime(),
                    CreatedBy = userName
                });

                await _dbContext.SaveChangesAsync();
                response.Add("status", "success");
                response.Add("message", "Stock updated successfully.");
            }
            catch (Exception ex)
            {
                response.Add("status", "error");
                response.Add("message", "Error: " + ex.Message);
            }

            return Json(response);
        }

        // View Transaction Logs
        public async Task<IActionResult> Transactions(string userId)
        {
            var query = _dbContext.InventoryTransactions.Include(t => t.FoodItem).AsQueryable();
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(t => t.UserId == userId);
            }

            var logs = await query.OrderByDescending(t => t.EntryDate).Take(500).ToListAsync();
            return View(logs);
        }
    }
}
