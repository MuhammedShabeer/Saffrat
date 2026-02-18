using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Helpers;
using Saffrat.Models;
using System.Security.Claims;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class UsersController : BaseController
    {
        private readonly ILogger<UsersController> _logger;
        private readonly RestaurantDBContext _dbContext;

        public UsersController(ILogger<UsersController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService)
        : base(languageService, localizationService)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /*
         * User Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.loggedInUserName = userName;

            var users = await _dbContext.Users.ToListAsync();
            return View(users);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult AddUser()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult EditUser(int? Id)
        {
            var existing = _dbContext.Users.FirstOrDefault(x => x.Id == Id);
            if (existing != null)
            {
                existing.Password = "";
                return View(existing);
            }
            return NotFound();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult ViewUser(int? Id, string UserName)
        {
            var existing = _dbContext.Users.FirstOrDefault(x => x.Id == Id || x.UserName == UserName);
            if (existing != null)
            {
                return View(existing);
            }
            return NotFound();
        }

        /*
         * User APIs
         */
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddUser(User user)
        {
            var response = new Dictionary<string, string>();
            user.Email = user.Email.ToLower();
            user.UserName = user.UserName.ToLower();
            
            if (ModelState.IsValid)
            {
                var existing = _dbContext.Users.FirstOrDefault(x => x.Email == user.Email || x.UserName == user.UserName);
                if (existing != null)
                {
                    if (existing.Email == user.Email)
                    {
                        response.Add("status", "error");
                        response.Add("message", "Email already exist.");
                    }
                    else if (existing.UserName == user.UserName)
                    {
                        response.Add("status", "error");
                        response.Add("message", "Username already exist.");
                    }
                }
                else if (!IsRoleValid(user.Role))
                {
                    response.Add("status", "error");
                    response.Add("message", "Please select valid role.");
                }
                else
                {
                    var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    user.Status = user.Status == 1 ? 1 : 0;
                    user.Password = Encryption.GetMD5(user.Password);
                    user.UpdatedBy = userName;
                    user.UpdatedAt = CurrentDateTime();

                    try
                    {
                        _dbContext.Users.Add(user);
                        await _dbContext.SaveChangesAsync();
                        response.Add("status", "success");
                        response.Add("message", "success");
                    }
                    catch
                    {
                        response.Add("status", "error");
                        response.Add("message", "Error while saving user.");
                    }
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", FirstError(ModelState));
            }

            return Json(response);
        }

        [HttpPut]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateUser(User user)
        {
            var response = new Dictionary<string, string>();
            user.Email = user.Email.ToLower();
            user.UserName = user.UserName.ToLower();

            if (ModelState.IsValid)
            {
                var existing = _dbContext.Users.FirstOrDefault(x => x.Id != user.Id && (x.Email == user.Email || x.UserName == user.UserName));
                if (existing != null)
                {
                    if (existing.Email == user.Email)
                    {
                        response.Add("status", "error");
                        response.Add("message", "Email already exist.");
                    }
                    else if (existing.UserName == user.UserName)
                    {
                        response.Add("status", "error");
                        response.Add("message", "Username already exist.");
                    }
                }
                else if (!IsRoleValid(user.Role))
                {
                    response.Add("status", "error");
                    response.Add("message", "Please select valid role.");
                }
                else
                {
                    var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    user.Status = user.Status == 1 ? 1 : 0;
                    user.Password = Encryption.GetMD5(user.Password);
                    user.UpdatedBy = userName;
                    user.UpdatedAt = CurrentDateTime();

                    try
                    {
                        _dbContext.Users.Update(user);
                        await _dbContext.SaveChangesAsync();
                        response.Add("status", "success");
                        response.Add("message", "User updated successfully.");
                    }
                    catch
                    {
                        response.Add("status", "error");
                        response.Add("message", "Error while updating user.");
                    }
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", FirstError(ModelState));
            }

            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteUser(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Users.FindAsync(Id);
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (existing != null)
            {
                if (existing.UserName == userName)
                {
                    response.Add("status", "error");
                    response.Add("message", "Something went wrong.");
                }
                else
                {
                    try
                    {
                        _dbContext.Users.Remove(existing);
                        await _dbContext.SaveChangesAsync();

                        response.Add("status", "success");
                        response.Add("message", "success");
                    }
                    catch
                    {
                        response.Add("status", "error");
                        response.Add("message", "Error while deleting user.");
                    }
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "User not exist.");
            }
            return Json(response);
        }

        // Class Private Functions

        private static bool IsRoleValid(string role)
        {
            string[] roles = { "admin","staff","waiter","deliveryman"};
            return roles.Contains(role);
        }
    }
}