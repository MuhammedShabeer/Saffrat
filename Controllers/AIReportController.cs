using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Services;
using System.Text;

namespace Saffrat.Controllers
{
    [Authorize(Roles = "admin")]
    public class AIReportController : BaseController
    {
        private readonly IGeminiAIService _aiService;
        private readonly RestaurantDBContext _dbContext;

        public AIReportController(IGeminiAIService aiService, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService)
            : base(languageService, localizationService)
        {
            _aiService = aiService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.isModelLoaded = !string.IsNullOrEmpty(GetSetting?.GeminiApiKey);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
                return BadRequest("Message is empty");

            if (string.IsNullOrEmpty(GetSetting?.GeminiApiKey))
                return Json(new { response = "Please configure your Gemini API Key in General Settings first." });

            // 1. Load Schema Context
            string schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "AppFiles", "AI", "SchemaGuide.txt");
            string schema = System.IO.File.Exists(schemaPath) ? await System.IO.File.ReadAllTextAsync(schemaPath) : "";

            // 2. Load Data Context
            string context = await PrepareContextAsync();

            string systemPrompt = $"You are a sophisticated Restaurant Business Intelligence Analyst. Below is the structure of the restaurant's database (Schema) and a snapshot of current data (Context). Use both to answer the user's question accurately. If the user asks for a report that requires more detail than provided, explain what data is available based on the schema.\n\n[DATABASE SCHEMA]\n{schema}\n\n[DATA CONTEXT]\n{context}\n\nUser Question: {request.Message}";

            string response = await _aiService.GetResponseAsync(systemPrompt, GetSetting.GeminiApiKey);

            return Json(new { response = response });
        }

        private async Task<string> PrepareContextAsync()
        {
            var sb = new StringBuilder();
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            sb.AppendLine("### COMPREHENSIVE BUSINESS SNAPSHOT ###");
            sb.AppendLine($"Report Date: {DateTime.Now:F}");

            // 1. SALES & ORDERS
            sb.AppendLine("\n[SECTION: SALES & ORDERS]");
            var salesToday = await _dbContext.Orders.Where(x => x.CreatedAt >= today).CountAsync();
            var revenueToday = await _dbContext.Orders.Where(x => x.CreatedAt >= today).SumAsync(x => (decimal?)x.Total) ?? 0;
            var revenueMonth = await _dbContext.Orders.Where(x => x.CreatedAt >= startOfMonth).SumAsync(x => (decimal?)x.Total) ?? 0;

            sb.AppendLine($"- Orders Today: {salesToday}");
            sb.AppendLine($"- Revenue Today: {revenueToday:C}");
            sb.AppendLine($"- Revenue This Month: {revenueMonth:C}");

            var topItems = await _dbContext.OrderDetails
                .GroupBy(x => x.Item.ItemName)
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Qty)
                .Take(5).ToListAsync();
            sb.AppendLine("- Top 5 Items: " + string.Join(", ", topItems.Select(x => $"{x.Name}({x.Qty})")));

            // 2. INVENTORY
            sb.AppendLine("\n[SECTION: INVENTORY]");
            var totalIngredients = await _dbContext.IngredientItems.CountAsync();
            var lowStockItems = await _dbContext.IngredientItems.Where(x => x.Quantity <= x.AlertQuantity).ToListAsync();
            sb.AppendLine($"- Total Ingredients: {totalIngredients}");
            sb.AppendLine($"- Low Stock Count: {lowStockItems.Count}");
            if (lowStockItems.Any())
                sb.AppendLine("- Critical Items: " + string.Join(", ", lowStockItems.Take(5).Select(x => x.ItemName)));

            // 3. CUSTOMERS
            sb.AppendLine("\n[SECTION: CUSTOMERS]");
            var totalCustomers = await _dbContext.Customers.CountAsync();
            var topCustomers = await _dbContext.Orders.Include(x => x.Customer)
                .GroupBy(x => x.Customer.CustomerName)
                .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Total) })
                .OrderByDescending(x => x.Total).Take(3).ToListAsync();
            sb.AppendLine($"- Total Customers in Database: {totalCustomers}");
            sb.AppendLine("- Top Customers: " + string.Join(", ", topCustomers.Select(x => x.Name)));

            // 4. HRM & STAFF
            sb.AppendLine("\n[SECTION: STAFF & ATTENDANCE]");
            var totalEmployees = await _dbContext.Employees.CountAsync();
            var presentToday = await _dbContext.Attendances.Where(x => x.AttendaceDate == today).CountAsync();
            sb.AppendLine($"- Total Staff: {totalEmployees}");
            sb.AppendLine($"- Present Today: {presentToday}");

            // 5. FINANCE & PURCHASES
            sb.AppendLine("\n[SECTION: FINANCE]");
            var monthPurchases = await _dbContext.Purchases.Where(x => x.PurchaseDate >= startOfMonth).SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
            var topSuppliers = await _dbContext.Purchases.Include(x => x.Supplier)
                .GroupBy(x => x.Supplier.SupplierName)
                .Select(g => new { Name = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .OrderByDescending(x => x.Total).Take(3).ToListAsync();

            sb.AppendLine($"- Total Purchases (This Month): {monthPurchases:C}");
            sb.AppendLine("- Main Suppliers: " + string.Join(", ", topSuppliers.Select(x => x.Name)));

            // 6. ASSETS
            sb.AppendLine("\n[SECTION: ASSETS]");
            var totalTables = await _dbContext.RestaurantTables.CountAsync();
            sb.AppendLine($"- Total Dining Tables: {totalTables}");

            return sb.ToString();
        }

        public class ChatRequest
        {
            public string Message { get; set; }
        }
    }
}
