using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Helpers;
using Saffrat.Models;
using System.Security.Claims;
using Saffrat.Services;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;

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
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> FoodGroups()
        {
            var groups = await _dbContext.FoodGroups.ToListAsync();
            return View(groups);
        }

        /*
         * Food Group APIs
         */

        [HttpPost]
        [Authorize(Roles = "admin,staff")]
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
        [Authorize(Roles = "admin,staff")]
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
        [Authorize(Roles = "admin,staff")]
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
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> FoodItems()
        {
            var items = await _dbContext.FoodItems.OrderByDescending(x => x.Id)
                .Include(x => x.Group).ToListAsync();
            return View(items);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult AddFoodItem()
        {
            ViewBag.ingredients = GetIngredients();
            ViewBag.foodgroups = GetFoodGroups();
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
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
        [Authorize(Roles = "admin,staff")]
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
        [Authorize(Roles = "admin,staff")]
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
        [Authorize(Roles = "admin,staff")]
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

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> DownloadFoodExcel()
        {
            var items = await _dbContext.FoodItems.Include(x => x.Group).ToListAsync();
            
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("FoodItems");
                
                // Headers
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 2).Value = "GroupName";
                worksheet.Cell(1, 3).Value = "ItemName";
                worksheet.Cell(1, 4).Value = "ArabicName";
                worksheet.Cell(1, 5).Value = "Description";
                worksheet.Cell(1, 6).Value = "Price";
                worksheet.Cell(1, 7).Value = "VanSalePrice";
                worksheet.Cell(1, 8).Value = "WholeSalePrice";
                worksheet.Cell(1, 9).Value = "Barcode";
                worksheet.Cell(1, 10).Value = "PermittedSalesTypes";
                worksheet.Cell(1, 11).Value = "Image";

                // Header styling
                var headerRange = worksheet.Range(1, 1, 1, 11);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Data
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    int row = i + 2;
                    worksheet.Cell(row, 1).Value = item.Id ?? 0;
                    worksheet.Cell(row, 2).Value = item.Group?.GroupName;
                    worksheet.Cell(row, 3).Value = item.ItemName;
                    worksheet.Cell(row, 4).Value = item.ArabicName;
                    worksheet.Cell(row, 5).Value = item.Description;
                    worksheet.Cell(row, 6).Value = item.Price;
                    worksheet.Cell(row, 7).Value = item.VanSalePrice;
                    worksheet.Cell(row, 8).Value = item.WholeSalePrice;
                    worksheet.Cell(row, 9).Value = item.Barcode;
                    worksheet.Cell(row, 10).Value = item.PermittedSalesTypes;
                    worksheet.Cell(row, 11).Value = item.Image;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Saffrat_Menu_Excel.xlsx");
                }
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> ImportFoodItems(IFormFile menuFile)
        {
            var results = new Dictionary<string, string>();
            if (menuFile == null || menuFile.Length == 0)
            {
                results.Add("status", "error");
                results.Add("message", "Please select a valid Excel or CSV file.");
                return Json(results);
            }

            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groups = await _dbContext.FoodGroups.ToListAsync();
            int inserted = 0;
            int updated = 0;
            int errors = 0;

            try
            {
                var extension = Path.GetExtension(menuFile.FileName).ToLower();
                if (extension == ".xlsx")
                {
                    using (var workbook = new XLWorkbook(menuFile.OpenReadStream()))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                        foreach (var row in rows)
                        {
                            try
                            {
                                int? id = null;
                                if (!row.Cell(1).IsEmpty()) id = row.Cell(1).GetValue<int>();
                                
                                var groupName = row.Cell(2).GetValue<string>();
                                var itemName = row.Cell(3).GetValue<string>();

                                if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(itemName)) { errors++; continue; }

                                var group = groups.FirstOrDefault(x => x.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                                if (group == null) { errors++; continue; }

                                FoodItem item = null;
                                if (id.HasValue && id > 0) item = await _dbContext.FoodItems.FindAsync(id.Value);

                                bool isNew = false;
                                if (item == null) { item = new FoodItem(); isNew = true; }

                                item.GroupId = (int)group.Id;
                                item.ItemName = itemName;
                                item.ArabicName = row.Cell(4).GetValue<string>();
                                item.Description = row.Cell(5).GetValue<string>();
                                item.Price = row.Cell(6).GetValue<decimal>();
                                item.VanSalePrice = row.Cell(7).GetValue<decimal>();
                                item.WholeSalePrice = row.Cell(8).GetValue<decimal>();
                                item.Barcode = row.Cell(9).GetValue<string>();
                                item.PermittedSalesTypes = row.Cell(10).GetValue<string>();
                                item.Image = row.Cell(11).IsEmpty() ? "default" : row.Cell(11).GetValue<string>();
                                item.UpdatedAt = CurrentDateTime();
                                item.UpdatedBy = userName;

                                if (isNew) { _dbContext.FoodItems.Add(item); inserted++; }
                                else { _dbContext.FoodItems.Update(item); updated++; }
                            }
                            catch { errors++; }
                        }
                    }
                }
                else if (extension == ".csv")
                {
                    using (var reader = new StreamReader(menuFile.OpenReadStream(), Encoding.UTF8))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csv.Read();
                        csv.ReadHeader();
                        while (csv.Read())
                        {
                            try
                            {
                                var id = csv.GetField<int?>("Id");
                                var groupName = csv.GetField<string>("GroupName");
                                var itemName = csv.GetField<string>("ItemName");

                                if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(itemName)) { errors++; continue; }

                                var group = groups.FirstOrDefault(x => x.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                                if (group == null) { errors++; continue; }

                                FoodItem item = null;
                                if (id.HasValue && id > 0) item = await _dbContext.FoodItems.FindAsync(id.Value);

                                bool isNew = false;
                                if (item == null) { item = new FoodItem(); isNew = true; }

                                item.GroupId = (int)group.Id;
                                item.ItemName = itemName;
                                item.ArabicName = csv.GetField<string>("ArabicName");
                                item.Description = csv.GetField<string>("Description");
                                item.Price = csv.GetField<decimal>("Price");
                                item.VanSalePrice = csv.GetField<decimal>("VanSalePrice");
                                item.WholeSalePrice = csv.GetField<decimal>("WholeSalePrice");
                                item.Barcode = csv.GetField<string>("Barcode");
                                item.PermittedSalesTypes = csv.GetField<string>("PermittedSalesTypes");
                                item.Image = csv.GetField<string>("Image") ?? "default";
                                item.UpdatedAt = CurrentDateTime();
                                item.UpdatedBy = userName;

                                if (isNew) { _dbContext.FoodItems.Add(item); inserted++; }
                                else { _dbContext.FoodItems.Update(item); updated++; }
                            }
                            catch { errors++; }
                        }
                    }
                }
                else
                {
                    results.Add("status", "error");
                    results.Add("message", "Unsupported file format. Please use Excel (.xlsx) or CSV.");
                    return Json(results);
                }

                await _dbContext.SaveChangesAsync();
                results.Add("status", "success");
                results.Add("message", $"Import complete: {inserted} inserted, {updated} updated, {errors} skipped due to errors.");
            }
            catch (Exception ex)
            {
                results.Add("status", "error");
                results.Add("message", "Error during file processing: " + ex.Message);
            }

            return Json(results);
        }

        /*
         * Modifiers Views
         */

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> Modifiers()
        {
            var items = await _dbContext.Modifiers.OrderByDescending(x => x.Id).ToListAsync();
            return View(items);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult AddModifier()
        {
            ViewBag.Ingredients = GetIngredients();
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
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
        [Authorize(Roles = "admin,staff")]
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
        [Authorize(Roles = "admin,staff")]
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
        [Authorize(Roles = "admin,staff")]
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