using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class WaiterController : BaseController
    {
        private readonly ILogger<WaiterController> _logger;
        private readonly RestaurantDBContext _dbContext;

        public WaiterController(ILogger<WaiterController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
        : base(languageService, localizationService, dateTimeService)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        [Authorize(Roles = "waiter")]
        public async Task<IActionResult> Index()
        {
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _dbContext.RunningOrders.Where(x => x.WaiterOrDriver == userName)
                    .Include(x => x.Customer)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.RunningOrderItemModifiers)
                    .ThenInclude(x => x.Modifier).ToListAsync();
            return View(orders);
        }
        [HttpGet]
        [Authorize(Roles = "waiter")]
        public IActionResult GetOrder(int? Id)
        {
            var response = new Dictionary<string, string>();
            JsonSerializerOptions options = new()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            };
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var order = _dbContext.RunningOrders.Where(x => x.Id == Id && x.WaiterOrDriver == userName)
                    .FirstOrDefault();

                if (order != null)
                {
                    response.Add("order", JsonSerializer.Serialize(order,options));
                    response.Add("status", "success");
                    response.Add("message", "");
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Order not exist.");
                }
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "waiter")]
        public IActionResult OrderDetail(int? Id)
        {
            var response = new Dictionary<string, string>();
            JsonSerializerOptions options = new()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            };
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var order = _dbContext.RunningOrders.Where(x => x.Id == Id && x.WaiterOrDriver == userName && x.Status > 0 && x.Status < 4)
                    .Include(x => x.Customer)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.RunningOrderDetails)
                    .ThenInclude(x => x.RunningOrderItemModifiers)
                    .ThenInclude(x => x.Modifier)
                    .FirstOrDefault();

                if (order != null)
                {
                    response.Add("order", JsonSerializer.Serialize(order, options));
                    response.Add("status", "success");
                    response.Add("message", "");
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Order not exist.");
                }
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }
    }
}