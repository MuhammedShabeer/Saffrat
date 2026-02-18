using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Helpers;
using Saffrat.Models;
using System.Security.Claims;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class TableController : BaseController
    {
        private readonly ILogger<TableController> _logger;
        private readonly RestaurantDBContext _dbContext;

        public TableController(ILogger<TableController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService)
        : base(languageService, localizationService)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /*
         * Restaurant Table Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var tables = await _dbContext.RestaurantTables.ToListAsync();
            return View(tables);
        }

        /*
         * Restaurant Table APIs
         */
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveTable(RestaurantTable table, IFormFile tableimage)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                table.UpdatedAt = CurrentDateTime();
                table.UpdatedBy = userName;
                table.Image = "default";

                if (tableimage != null)
                {
                    var res = Uploader.UploadImageMedia(1, "Tables", tableimage);
                    if (res["status"] == "success")
                    {
                        table.Image = res["message"];
                    }
                    else
                    {
                        return Json(res);
                    }
                }

                if (table.Id > 0)
                {
                    if(IsTableExist(table.Id, table.TableName))
                    {
                        response.Add("status", "error");
                        response.Add("message", "Table name already exist.");
                    }
                    else
                    {
                        try
                        {
                            var existing = _dbContext.RestaurantTables.AsNoTracking().FirstOrDefault(x => x.Id == table.Id);
                            table.Image = tableimage == null ? existing.Image : table.Image;

                            _dbContext.RestaurantTables.Update(table);
                            await _dbContext.SaveChangesAsync();

                            response.Add("status", "success");
                            response.Add("message", "success");
                        }
                        catch
                        {
                            response.Add("status", "error");
                            response.Add("message", "Error while updating table.");
                        }
                    }
                }
                else
                {
                    if (IsTableExist(table.TableName))
                    {
                        response.Add("status", "error");
                        response.Add("message", "Table name already exist.");
                    }
                    else
                    {
                        try
                        {
                            _dbContext.RestaurantTables.Add(table);
                            await _dbContext.SaveChangesAsync();

                            response.Add("status", "success");
                            response.Add("message", "success");
                        }
                        catch
                        {
                            response.Add("status", "error");
                            response.Add("message", "Error while saving table.");
                        }
                    }
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
        public IActionResult TableDetail(int? Id)
        {
            var response = new Dictionary<string, string>();
            var row = _dbContext.RestaurantTables.FirstOrDefault(x => x.Id == Id);
            if (row != null)
            {
                return Json(new
                {
                    status = "success",
                    data = row
                });
            }
            else
            {
                return Json(new
                {
                    status = "error",
                    data = "Table not exist."
                });
            }
        }

        [HttpDelete]
		[Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteTable(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.RestaurantTables.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    _dbContext.RestaurantTables.Remove(existing);
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
                    response.Add("message", "Error while deleting table.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Table not exist.");
            }
            
            return Json(response);
        }

        [HttpGet]
		[Authorize(Roles = "admin,staff")]
        public IActionResult AvailableTables()
        {
            try
            {
                var tables = _dbContext.RestaurantTables.Where(x => x.Status.Equals(false));

                return Json(new
                {
                    status = "success",
                    data = tables
                });
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


        //private functions
        private bool IsTableExist(string name)
        {
            var table = _dbContext.RestaurantTables.FirstOrDefault(x=>x.TableName == name);
            return table != null;
        }
        private bool IsTableExist(int? id, string name)
        {
            var table = _dbContext.RestaurantTables.FirstOrDefault(x => x.TableName == name && x.Id != id);
            return table != null;
        }
    }
}