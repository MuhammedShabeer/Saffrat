using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Helpers;
using Saffrat.Models;
using System.Security.Claims;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class FoodController : BaseController
    {
		private readonly ILogger<FoodController> _logger;
		private readonly RestaurantDBContext _dbContext;

        public FoodController(ILogger<FoodController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
        : base(languageService, localizationService, dateTimeService)
        {
			_logger = logger;
			_dbContext = dbContext;
		}

		/*
         *  Food Group Views
         */

		[HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> FoodGroups()
        {
            var groups = await _dbContext.FoodGroups.ToListAsync();
            return View(groups);
        }

        /*
         * Food Group APIs
         */

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveFoodGroup(FoodGroup group, IFormFile groupImage)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                var existing = _dbContext.FoodGroups.AsNoTracking().FirstOrDefault(x => x.Id == group.Id);
                group.Image = "default";
                if (groupImage != null)
                {
                    var res = Uploader.UploadImageMedia(1, "FoodGroups", groupImage);
                    if (res["status"] == "success")
                    {
                        group.Image = res["message"];
                    }
                    else
                    {
                        return Json(res);
                    }
                }

                group.UpdatedAt = CurrentDateTime();
                group.UpdatedBy = userName;

                if (group.Id > 0)
                {
                    group.Image = groupImage == null ? existing.Image : group.Image;

                    _dbContext.FoodGroups.Update(group);
                }
                else
                {
                    _dbContext.FoodGroups.Add(group);
                }

                try
                {
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving food group.");
                }
            }
            else
            {
                response.Add("status", "error");
				response.Add("message", "Enter required fields.");
			}

            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult FoodGroupDetail(int? Id)
        {
            try
            {
                var row = _dbContext.FoodGroups.FirstOrDefault(x => x.Id == Id);
                if (row != null)
                {
                    return Json(new
                    {
                        data = row,
                        status = "success",
                        message = "success"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Food Group not exist."
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

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteFoodGroup(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.FoodGroups.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    _dbContext.FoodGroups.Remove(existing);
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
                    response.Add("message", "Error while deleting food group.");
                }
            }
            else
            {
                response.Add("status", "error");
				response.Add("message", "Food Group not exist.");
			}
            
            return Json(response);
        }

        /*
         * Food Item Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> FoodItems()
        {
            var items = await _dbContext.FoodItems.OrderByDescending(x => x.Id)
                .Include(x => x.Group).ToListAsync();
            return View(items);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult AddFoodItem()
        {
            ViewBag.ingredients = GetIngredients();
            ViewBag.foodgroups = GetFoodGroups();
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult EditFoodItem(int? Id)
        {
            var item = _dbContext.FoodItems.FirstOrDefault(x => x.Id == Id);
            if (item != null)
            {
                item.FoodItemIngredients = _dbContext.FoodItemIngredients.Where(x => x.FoodItemId == item.Id)
                    .Include(x => x.Ingredient)
                    .ToList();
                ViewBag.Ingredients = GetIngredients();
                ViewBag.foodgroups = GetFoodGroups();

                return View(item);
            }
            return NotFound();
        }

        /*
         * Food Item APIs
         */
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddFoodItem(FoodItem foodItem, int?[] ItemId, decimal?[] ItemConsumption, IFormFile itemimage)
        {
            var response = new Dictionary<string, string>();
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (ModelState.IsValid)
                {
                    if (itemimage != null)
                    {
                        var res = Uploader.UploadImageMedia(1, "Products", itemimage);
                        if (res["status"] == "success")
                        {
                            foodItem.Image = res["message"];
                        }
                        else
                        {
                            return Json(res);
                        }
                    }
                    else
                        foodItem.Image = "default";

                    foodItem.UpdatedAt = CurrentDateTime();
                    foodItem.UpdatedBy = userName;
                    _dbContext.FoodItems.Add(foodItem);
                    _dbContext.SaveChanges();

                    if (ItemId.Length > 0 && ItemId.Length == ItemConsumption.Length)
                    {
                        for (int i = 0; i < ItemId.Length; i++)
                        {
                            var item = _dbContext.IngredientItems.FirstOrDefault(x => x.Id == ItemId[i]);
                            if (item != null)
                            {
                                FoodItemIngredient foodItemIngredient = new()
                                {
                                    FoodItemId = Convert.ToInt32(foodItem.Id),
                                    IngredientId = Convert.ToInt32(item.Id),
                                    Quantity = Convert.ToDecimal(ItemConsumption[i])
                                };
                                _dbContext.FoodItemIngredients.Add(foodItemIngredient);
                            }
                        }
                    }

                    _dbContext.SaveChanges();
                    await transaction.CommitAsync();

                    response.Add("status", "success");
					response.Add("message", "success");
				}
                else
                {
                    response.Add("status", "error");
					response.Add("message", "Enter required fields.");
				}
            }
            catch
            {
                await transaction.RollbackAsync();
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }
            return Json(response);
        }

        [HttpPut]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateFoodItem(FoodItem foodItem, int?[] ItemId, decimal?[] ItemConsumption, IFormFile itemimage)
        {
            var response = new Dictionary<string, string>();
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (ModelState.IsValid)
                {
                    var existing = _dbContext.FoodItems.AsNoTracking().FirstOrDefault(x=> x.Id == foodItem.Id);
                    if (itemimage != null)
                    {
                        var res = Uploader.UploadImageMedia(1, "Products", itemimage);
                        if (res["status"] == "success")
                        {
                            foodItem.Image = res["message"];
                        }
                        else
                        {
                            return Json(res);
                        }
                    }

                    foodItem.Image = itemimage == null ? existing.Image : foodItem.Image;
                    foodItem.UpdatedAt = CurrentDateTime();
                    foodItem.UpdatedBy = userName;
                    _dbContext.FoodItems.Update(foodItem);
                    _dbContext.SaveChanges();

                    foreach (var row in _dbContext.FoodItemIngredients.Where(x => x.FoodItemId == foodItem.Id))
                    {
                        _dbContext.FoodItemIngredients.Remove(row);
                    }
                    _dbContext.SaveChanges();

                    if (ItemId.Length > 0 && ItemId.Length == ItemConsumption.Length)
                    {
                        for (int i = 0; i < ItemId.Length; i++)
                        {
                            var item = _dbContext.IngredientItems.FirstOrDefault(x => x.Id == ItemId[i]);
                            if (item != null)
                            {
                                FoodItemIngredient foodItemIngredient = new()
                                {
                                    FoodItemId = Convert.ToInt32(foodItem.Id),
                                    IngredientId = Convert.ToInt32(item.Id),
                                    Quantity = Convert.ToDecimal(ItemConsumption[i])
                                };
                                _dbContext.FoodItemIngredients.Add(foodItemIngredient);
                            }
                        }
                    }

                    _dbContext.SaveChanges();
                    await transaction.CommitAsync();

                    response.Add("status", "success");
					response.Add("message", "success");
				}
                else
                {
                    response.Add("status", "error");
					response.Add("message", "Enter required fields.");
				}
            }
            catch
            {
                await transaction.RollbackAsync();
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }
            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteFoodItem(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.FoodItems.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    _dbContext.FoodItems.Remove(existing);
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
                    response.Add("message", "Error while deleting food item.");
                }
            }
            else
            {
                response.Add("status", "error");
				response.Add("message", "Food Item not exist.");
			}
            
            return Json(response);
        }

        /*
         * Modifiers Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Modifiers()
        {
            var items = await _dbContext.Modifiers.OrderByDescending(x => x.Id).ToListAsync();
            return View(items);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult AddModifier()
        {
            ViewBag.Ingredients = GetIngredients();
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult EditModifier(int? Id)
        {
            var item = _dbContext.Modifiers.FirstOrDefault(x => x.Id == Id);
            if (item != null)
            {
                item.ModifierIngredients = _dbContext.ModifierIngredients.Where(x => x.ModifierId == item.Id)
                    .Include(x => x.Ingredient)
                    .ToList();
                ViewBag.Ingredients = GetIngredients();
                return View(item);
            }
            return NotFound();
        }

        /*
         * Modifiers APIs
         */
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddModifier(Modifier modifier, int?[] ItemId, decimal?[] ItemConsumption)
        {
            var response = new Dictionary<string, string>();
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (ModelState.IsValid)
                {
                    modifier.UpdatedAt = CurrentDateTime();
                    modifier.UpdatedBy = userName;
                    _dbContext.Modifiers.Add(modifier);
                    _dbContext.SaveChanges();

                    if (ItemId.Length > 0 && ItemId.Length == ItemConsumption.Length)
                    {
                        for (int i = 0; i < ItemId.Length; i++)
                        {
                            var item = _dbContext.IngredientItems.FirstOrDefault(x => x.Id == ItemId[i]);
                            if (item != null)
                            {
                                ModifierIngredient modifierIngredient = new()
                                {
                                    ModifierId = Convert.ToInt32(modifier.Id),
                                    IngredientId = Convert.ToInt32(item.Id),
                                    Quantity = Convert.ToDecimal(ItemConsumption[i])
                                };
                                _dbContext.ModifierIngredients.Add(modifierIngredient);
                            }
                        }
                    }

                    _dbContext.SaveChanges();
                    await transaction.CommitAsync();

                    response.Add("status", "success");
					response.Add("message", "success");
				}
                else
                {
                    response.Add("status", "error");
					response.Add("message", "Enter required fields.");
				}
            }
            catch
            {
                await transaction.RollbackAsync();
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }
            return Json(response);
        }

        [HttpPut]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateModifier(Modifier modifier, int?[] ItemId, decimal?[] ItemConsumption)
        {
            var response = new Dictionary<string, string>();
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (ModelState.IsValid)
                {
                    modifier.UpdatedAt = CurrentDateTime();
                    modifier.UpdatedBy = userName;
                    _dbContext.Modifiers.Update(modifier);
                    _dbContext.SaveChanges();

                    foreach (var row in _dbContext.ModifierIngredients.Where(x => x.ModifierId == modifier.Id))
                    {
                        _dbContext.ModifierIngredients.Remove(row);
                    }
                    _dbContext.SaveChanges();

                    if (ItemId.Length > 0 && ItemId.Length == ItemConsumption.Length)
                    {
                        for (int i = 0; i < ItemId.Length; i++)
                        {
                            var item = _dbContext.IngredientItems.FirstOrDefault(x => x.Id == ItemId[i]);
                            if (item != null)
                            {
                                ModifierIngredient modifierIngredient = new()
                                {
                                    ModifierId = Convert.ToInt32(modifier.Id),
                                    IngredientId = Convert.ToInt32(item.Id),
                                    Quantity = Convert.ToDecimal(ItemConsumption[i])
                                };
                                _dbContext.ModifierIngredients.Add(modifierIngredient);
                            }
                        }
                    }

                    _dbContext.SaveChanges();
                    await transaction.CommitAsync();
                    response.Add("status", "success");
					response.Add("message", "success");
				}
                else
                {
                    response.Add("status", "error");
					response.Add("message", "Enter required fields.");
				}
            }
            catch
            {
                await transaction.RollbackAsync();
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }
            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteModifier(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Modifiers.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    _dbContext.Modifiers.Remove(existing);
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
                    response.Add("message", "Error while deleting modifier.");
                }
            }
            else
            {
                response.Add("status", "error");
				response.Add("message", "Modifier not exist.");
			}
            
            return Json(response);
        }

        /*
         * Class Private Functions
         */
        private Dictionary<int, string> GetFoodGroups()
        {
            Dictionary<int, string> groups = _dbContext.FoodGroups
                .Where(x => x.Status.Equals(true))
                .Select(t => new
                {
                    t.Id,
                    t.GroupName
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.GroupName);
            return groups;
        }

        private Dictionary<int, string> GetIngredients()
        {
            Dictionary<int, string> ingredients = _dbContext.IngredientItems
                .Select(t => new
                {
                    t.Id,
                    t.ItemName
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.ItemName);
            return ingredients;
        }
    }
}