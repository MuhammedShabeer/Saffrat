using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Saffrat.Helpers;
using Saffrat.Models;
using Saffrat.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Saffrat.Services;
using Microsoft.AspNetCore.Localization;

namespace Saffrat.Controllers
{
    public class AccountController : BaseController
    {
        private readonly ILogger<AccountController> _logger;
        private readonly RestaurantDBContext _dbContext;

        public AccountController(ILogger<AccountController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
        : base(languageService, localizationService, dateTimeService)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /*
         * User Login
         * 
         */

        [HttpGet]
        public IActionResult Login()
        {
            if (User.IsInRole("admin"))
            {
                return RedirectToAction("Index", "Home");
            }
            else if (User.IsInRole("staff"))
            {
                return RedirectToAction("Index", "POS");
            }
            else if (User.IsInRole("waiter"))
            {
                return RedirectToAction("Index", "Waiter");
            }
            else if (User.IsInRole("deliveryman"))
            {
                return RedirectToAction("Index", "DeliveryMan");
            }

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login([Required] string email, [Required] string password, int? remember)
        {
            string message;
            if (ModelState.IsValid)
            {
                var user = _dbContext.Users.FirstOrDefault(x => (x.Email == email || x.UserName == email) && x.Status == 1);
                if (user != null && user.Password == Encryption.GetMD5(password))
                {
                    bool rem = remember != null;

                    var expiry = rem ? DateTime.UtcNow.AddDays(3) : DateTime.UtcNow.AddHours(1);
                    var claims = new List<Claim>() {
                        new Claim(ClaimTypes.NameIdentifier, user.UserName),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Role, user.Role)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties()
                    {
                        IsPersistent = rem,
                        ExpiresUtc = expiry,
                        AllowRefresh = true
                    });

                    user.LastLogin = _dateTimeService.Now();
                    _dbContext.Update(user);
                    await _dbContext.SaveChangesAsync();

                    AuditLog log = new()
                    {
                        Username = user.UserName,
                        Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                        Service = "Account",
                        Action = "Login",
                        Status = "success",
                        CreatedAt = CurrentDateTime(),
                        Description = "-"
                    };
                    SaveLog(log, _dbContext);

                    if (user.Role == "admin")
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else if (user.Role == "staff")
                    {
                        return RedirectToAction("Index", "POS");
                    }
                    else if (user.Role == "waiter")
                    {
                        return RedirectToAction("Index", "Waiter");
                    }
                    else if (user.Role == "deliveryman")
                    {
                        return RedirectToAction("Index", "DeliveryMan");
                    }
                }
            }
            else
            {
                AuditLog log = new()
                {
                    Username = "-",
                    Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                    Service = "Account",
                    Action = "Login",
                    Status = "error",
                    CreatedAt = CurrentDateTime(),
                    Description = "Submitted data is not valid."
                };
                SaveLog(log, _dbContext);
            }

            message = "Enter valid credentials.";
            ViewBag.Msg = message;
            return View();
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        /*
         * 
         * Change Password
         * 
         */
        [HttpGet]
        [Authorize(Roles = "admin,staff,waiter,deliveryman")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,staff,waiter,deliveryman")]
        public IActionResult ChangePassword(ChangePasswordVM pass)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _dbContext.Users.FirstOrDefault(s => s.UserName == userName);

            if (ModelState.IsValid)
            {
                if (user != null)
                {
                    if (user.Password != Encryption.GetMD5(pass.OldPassword))
                    {
                        response.Add("status", "error");
                        response.Add("message", "Enter valid old password");

                        AuditLog log = new()
                        {
                            Username = "-",
                            Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                            Service = "Account",
                            Action = "Change Password",
                            Status = "error",
                            CreatedAt = CurrentDateTime(),
                            Description = "Submitted data is not valid."
                        };
                        SaveLog(log, _dbContext);
                    }
                    else
                    {
                        user.Password = Encryption.GetMD5(pass.NewPassword);
                        _dbContext.Users.Update(user);
                        _dbContext.SaveChanges();

                        response.Add("status", "success");
                        response.Add("message", "Password changed successfully.");

                        AuditLog log = new()
                        {
                            Username = userName,
                            Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                            Service = "Account",
                            Action = "Change Password",
                            Status = "success",
                            CreatedAt = CurrentDateTime(),
                            Description = "-"
                        };
                        SaveLog(log, _dbContext);
                    }
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", FirstError(ModelState));

                AuditLog log = new()
                {
                    Username = "-",
                    Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                    Service = "Account",
                    Action = "Change Password",
                    Status = "error",
                    CreatedAt = CurrentDateTime(),
                    Description = "Submitted data is not valid."
                };
                SaveLog(log, _dbContext);
            }

            return Json(response);
        }

        /*
         * Forgot Password
         */

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var results = new Dictionary<string, string>();
            var user = _dbContext.Users.FirstOrDefault(x => x.Email == email && x.Status.Equals(1));
            if (user != null)
            {
                var usertoken = _dbContext.UserTokens.FirstOrDefault(x => x.Username == user.UserName && x.TokenType == "Reset Password");
                if(usertoken == null)
                {
                    usertoken = new UserToken()
                    {
                        Username = user.UserName,
                        TokenType = "Reset Password",
                        Token = Guid.NewGuid().ToString(),
                        Expiry = CurrentDateTime().AddHours(12),
                        GeneratedAt = CurrentDateTime(),
                    };
                    _dbContext.UserTokens.Add(usertoken);
                }
                else
                {
                    usertoken.Username = user.UserName;
                    usertoken.Token = Guid.NewGuid().ToString();
                    usertoken.TokenType = "Reset Password";
                    usertoken.Expiry = CurrentDateTime().AddHours(12);
                    usertoken.GeneratedAt = CurrentDateTime();
                    _dbContext.UserTokens.Update(usertoken);
                }
                _dbContext.SaveChanges();

                var host = HttpContext.Request.Host;
                var template = _dbContext.EmailTemplates.FirstOrDefault(x => x.Name == "Reset Password");
                var isSend = SendEmail.ResetPassEmail(GetSetting, user.UserName, user.Email, usertoken.Token, String.Format("https://{0}",host), Localize("Reset Password"), template.Template, template.Subject);

                if (isSend)
                {
                    AuditLog log = new()
                    {
                        Username = user.UserName,
                        Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                        Service = "Account",
                        Action = "Forgot Password",
                        Status = "success",
                        CreatedAt = CurrentDateTime(),
                        Description = "Email sent successfully."
                    };
                    SaveLog(log, _dbContext);
                }
                else
                {
                    AuditLog log = new()
                    {
                        Username = user.UserName,
                        Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                        Service = "Account",
                        Action = "Forgot Password",
                        Status = "error",
                        CreatedAt = CurrentDateTime(),
                        Description = "Email failed to send."
                    };
                    SaveLog(log, _dbContext);
                }
            }
            else
            {
                AuditLog log = new()
                {
                    Username = "-",
                    Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                    Service = "Account",
                    Action = "Forgot Password",
                    Status = "error",
                    CreatedAt = CurrentDateTime(),
                    Description = email+" not exist."
                };
                SaveLog(log, _dbContext);
            }

            ViewBag.submitted = true;
            ViewBag.email = email;
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            var user = _dbContext.Users.FirstOrDefault(x=> x.Email == email && x.Status.Equals(1));
            if( user != null)
            {
                var date = CurrentDateTime();
                var userToken = _dbContext.UserTokens.FirstOrDefault(u => u.Username == user.UserName && u.Token == token && u.TokenType == "Reset Password" && u.Expiry > date);
                if(userToken != null)
                {
                    ViewBag.token = token;
                    ViewBag.email = email;
                    return View();
                }
                else
                {
                    AuditLog log = new()
                    {
                        Username = user.UserName,
                        Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                        Service = "Account",
                        Action = "Reset Password",
                        Status = "error",
                        CreatedAt = CurrentDateTime(),
                        Description = "Submitted data is not valid."
                    };
                    SaveLog(log, _dbContext);
                }
            }

            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        public IActionResult ResetPassword(NewPasswordVM pass, [Required] string token, [Required] string email)
        {
            if (ModelState.IsValid)
            {
                var user = _dbContext.Users.FirstOrDefault(x => x.Email == email && x.Status.Equals(1));
                if (user != null)
                {
                    var date = CurrentDateTime();
                    var userToken = _dbContext.UserTokens.FirstOrDefault(u => u.Username == user.UserName && u.Token == token && u.TokenType == "Reset Password" && u.Expiry > date);
                    if (userToken != null)
                    {
                        user.Password = Encryption.GetMD5(pass.NewPassword);
                        _dbContext.Users.Update(user);
                        _dbContext.SaveChanges();
                        _dbContext.UserTokens.Remove(userToken);
                        _dbContext.SaveChanges();
                        var host = HttpContext.Request.Host;
                        var template = _dbContext.EmailTemplates.FirstOrDefault(x => x.Name == "Reset Password Success");
                        var isSend = SendEmail.ResetPassSuccess(GetSetting, user.UserName, user.Email, String.Format("https://{0}", host), template.Template, template.Subject);

                        if (isSend)
                        {
                            AuditLog log = new()
                            {
                                Username = user.UserName,
                                Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                                Service = "Account",
                                Action = "Reset Password",
                                Status = "success",
                                CreatedAt = CurrentDateTime(),
                                Description = "Email sent successfully."
                            };
                            SaveLog(log, _dbContext);
                        }
                        else
                        {
                            AuditLog log = new()
                            {
                                Username = user.UserName,
                                Ip = HttpContext.Connection.RemoteIpAddress == null ? "-" : HttpContext.Connection.RemoteIpAddress.ToString(),
                                Service = "Account",
                                Action = "Reset Password",
                                Status = "error",
                                CreatedAt = CurrentDateTime(),
                                Description = "Email failed to send."
                            };
                            SaveLog(log, _dbContext);
                        }

                        return RedirectToAction("Login", "Account");
                    }
                }
            }
            else
            {
                ViewBag.token = token;
                ViewBag.email = email;
                ViewBag.Msg = FirstError(ModelState);
                return View(new {token, email});
            }

            return RedirectToAction("Login", "Account");
        }
    }
}
