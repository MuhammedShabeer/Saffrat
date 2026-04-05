using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using System.Security.Claims;
using Saffrat.Services;
using System.Data;

namespace Saffrat.Controllers
{
    public class IngredientController : BaseController
    {
		private readonly ILogger<IngredientController> _logger;
		private readonly RestaurantDBContext _dbContext;

        public IngredientController(ILogger<IngredientController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
        : base(languageService, localizationService, dateTimeService)
        {
			_logger = logger;
			_dbContext = dbContext;
		}

        /*
         * Ingredient Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var items = await _dbContext.IngredientItems.OrderByDescending(x => x.Id).ToListAsync();
            return View(items);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult AddIngredient()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult EditIngredient(int? Id)
        {
            var existing = _dbContext.IngredientItems.FirstOrDefault(x => x.Id == Id);
            if (existing != null)
            {
                return View(existing);
            }

            return NotFound();
        }

        /*
         * Ingredient APIs
        */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult IngredientItemDetail(int? Id)
        {
            try
            {
                var row = _dbContext.IngredientItems.FirstOrDefault(x => x.Id == Id);
                if (row != null)
                {
                    return Json(new
                    {
                        data = row,
                        status = "success",
                        message = ""
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Ingredient Item not exist."
                    });
                }
            }
            catch
            {
                return Json(new
                {
                    status = "error",
                    message = "Something went wrong."
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddIngredient(IngredientItem item)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                item.UpdatedAt = CurrentDateTime();
                item.UpdatedBy = userName;

                try
                {
                    _dbContext.IngredientItems.Add(item);
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving ingredient item.");
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
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateIngredient(IngredientItem item)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                item.UpdatedAt = CurrentDateTime();
                item.UpdatedBy = userName;

                try
                {
                    _dbContext.IngredientItems.Update(item);
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while updating ingredient item.");
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
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteIngredient(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.IngredientItems.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    _dbContext.IngredientItems.Remove(existing);
                    _dbContext.SaveChanges();
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
                    response.Add("message", "Error while deleting ingredient item.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Ingredient Item not exist.");
            }

            return Json(response);
        }
    }
}