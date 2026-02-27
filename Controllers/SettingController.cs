using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Helpers;
using Saffrat.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Globalization;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class SettingController : BaseController
    {
        private readonly ILogger<SettingController> _logger;
        private readonly RestaurantDBContext _dbContext;

        public SettingController(ILogger<SettingController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService)
        : base(languageService, localizationService)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /*
         * General Settings
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult General()
        {
            ViewBag.accounts = _dbContext.GLAccounts.ToList();
            ViewBag.customers = _dbContext.Customers.ToList();

            return View(GetSetting);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult Printer()
        {
            return View(GetSetting);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Printer(int PrinterMethod, int PrinterPaperWidth, IFormFile InvoiceLogo)
        {
            var response = new Dictionary<string, string>();
            if (GetSetting != null)
            {
                if (InvoiceLogo != null)
                {
                    string fileName = Uploader.UploadImage(InvoiceLogo);
                    if (fileName == null)
                    {
                        response.Add("status", "error");
                        response.Add("message", "Logo file not supported.");
                        return Json(response);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(GetSetting.InvoiceLogo))
                            Uploader.DeleteFile(GetSetting.InvoiceLogo);
                        GetSetting.InvoiceLogo = fileName;
                    }
                }

                GetSetting.PrinterMethod = PrinterMethod;
                GetSetting.PrinterPaperWidth = PrinterPaperWidth;

                _dbContext.AppSettings.Update(GetSetting);
                await _dbContext.SaveChangesAsync();

                response.Add("status", "success");
                response.Add("message", "success");
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }
            return Json(response);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> General([Required] string AppName,
            [Required] int DefaultCustomer, [Required] int DefaultOrderType, [Required] int SaleAccount, [Required] int PurchaseAccount, [Required] int PayrollAccount,
            [Required] string Copyright, [Required] bool SendInvoiceEmail, [Required] bool SkipKitchenOrder, string GeminiApiKey, IFormFile Logo, IFormFile Favicon, IFormFile Preloader)
        {
            var response = new Dictionary<string, string>();

            if (GetSetting != null && ModelState.IsValid)
            {
                GetSetting.GeminiApiKey = GeminiApiKey;
                if (Logo != null)
                {
                    string fileName = Uploader.UploadImage(Logo);
                    if (fileName == null)
                    {
                        response.Add("status", "error");
                        response.Add("message", "Logo file not supported.");
                        return Json(response);
                    }
                    else
                    {
                        Uploader.DeleteFile(GetSetting.Logo);
                        GetSetting.Logo = fileName;
                    }
                }
                if (Favicon != null)
                {
                    string fileName = Uploader.UploadImage(Favicon);
                    if (String.IsNullOrEmpty(fileName))
                    {
                        response.Add("status", "error");
                        response.Add("message", "Favicon file not supported.");
                        return Json(response);
                    }
                    else
                    {
                        Uploader.DeleteFile(GetSetting.Favicon);
                        GetSetting.Favicon = fileName;
                    }
                }
                if (Preloader != null)
                {
                    string fileName = Uploader.UploadImage(Preloader);
                    if (String.IsNullOrEmpty(fileName))
                    {
                        response.Add("status", "error");
                        response.Add("message", "Preloader file not supported.");
                        return Json(response);
                    }
                    else
                    {
                        Uploader.DeleteFile(GetSetting.Preloader);
                        GetSetting.Preloader = fileName;
                    }
                }
                if (IsAccountExist(SaleAccount))
                {
                    GetSetting.SaleAccount = SaleAccount;
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Sale Account not exist.");
                    return Json(response);
                }
                if (IsAccountExist(PurchaseAccount))
                {
                    GetSetting.PurchaseAccount = PurchaseAccount;
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Purchase Account not exist.");
                    return Json(response);
                }
                if (IsAccountExist(PayrollAccount))
                {
                    GetSetting.PayrollAccount = PayrollAccount;
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Sale Account not exist.");
                    return Json(response);
                }
                if (IsCustomerExist(DefaultCustomer))
                {
                    GetSetting.DefaultCustomer = DefaultCustomer;
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Payroll account not exist.");
                    return Json(response);
                }

                GetSetting.SendInvoiceEmail = SendInvoiceEmail;
                GetSetting.SkipKitchenOrder = SkipKitchenOrder;
                GetSetting.AppName = AppName;
                GetSetting.Copyright = Copyright;
                GetSetting.DefaultOrderType = DefaultOrderType;

                try
                {
                    _dbContext.AppSettings.Update(GetSetting);
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving general settings.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }

            return Json(response);
        }

        private bool IsAccountExist(int id)
        {
            var account = _dbContext.GLAccounts.FirstOrDefault(x => x.Id == id);
            return account != null;
        }
        private bool IsCustomerExist(int id)
        {
            var customer = _dbContext.Customers.FirstOrDefault(x => x.Id == id);
            return customer != null;
        }
        private bool IsLanguageExist(string name)
        {
            var lang = _dbContext.Languages.FirstOrDefault(x => x.Culture == name);
            return lang != null;
        }
        private static bool IsRegionExist(string name)
        {
            return CultureInfo.GetCultures(CultureTypes.SpecificCultures).Any(culture => string.Equals(culture.Name, name));
        }

        private static bool IsTimeZoneExist(string id)
        {
            try
            {
                TimeZoneInfo tzf;
                tzf = TimeZoneInfo.FindSystemTimeZoneById(id);
                return tzf != null;
            }
            catch
            {
                return false;
            }
        }


        /*
         * System Settings
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult System()
        {
            return View(GetSetting);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> System([Required] string CurrencyName, [Required] string CurrencySymbol, [Required] int CurrencyPosition,
            [Required] string Timezone, [Required] string DefaultLanguage, [Required] string DefaultRegion
            )
        {
            var response = new Dictionary<string, string>();

            if (GetSetting != null && ModelState.IsValid)
            {
                if (IsTimeZoneExist(Timezone))
                {
                    GetSetting.Timezone = Timezone;
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Time Zone not exist.");
                    return Json(response);
                }

                if (IsRegionExist(DefaultRegion))
                {
                    GetSetting.DefaultRegion = DefaultRegion;
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Region not exist.");
                    return Json(response);
                }

                if (IsLanguageExist(DefaultLanguage))
                {
                    GetSetting.DefaultLanguage = DefaultLanguage;
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Language not exist.");
                    return Json(response);
                }

                GetSetting.CurrencyPosition = CurrencyPosition;
                GetSetting.CurrencyName = CurrencyName;
                GetSetting.CurrencySymbol = CurrencySymbol;

                try
                {

                    _dbContext.AppSettings.Update(GetSetting);
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving system setting.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }

            return Json(response);
        }


        /*
         * Company Settings
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult Company()
        {
            return View(GetSetting);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Company([Required] string CompanyName, [Required] string CompanyEmail, [Required] string CompanyPhone, [Required] string CompanyAddress, [Required] string CompanyTaxNum)
        {
            var response = new Dictionary<string, string>();

            if (GetSetting != null && ModelState.IsValid)
            {
                GetSetting.CompanyName = CompanyName;
                GetSetting.CompanyEmail = CompanyEmail;
                GetSetting.CompanyPhone = CompanyPhone;
                GetSetting.CompanyAddress = CompanyAddress;
                GetSetting.CompanyTaxNum = CompanyTaxNum;

                try
                {
                    _dbContext.AppSettings.Update(GetSetting);
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving company details.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }

            return Json(response);
        }

        /*
         * Email Configuration
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult Email()
        {
            return View(GetSetting);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Email([Required] string MailEncryption, [Required] string MailProtocol, [Required] string MailHost, [Required] string MailUserName, [Required] string MailPassword, [Required] int MailPort)
        {
            var response = new Dictionary<string, string>();

            if (GetSetting != null && ModelState.IsValid)
            {
                GetSetting.MailEncryption = MailEncryption;
                GetSetting.MailProtocol = MailProtocol;
                GetSetting.MailHost = MailHost;
                GetSetting.MailUserName = MailUserName;
                GetSetting.MailPassword = MailPassword;
                GetSetting.MailPort = MailPort;

                try
                {
                    _dbContext.AppSettings.Update(GetSetting);
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving email configuration.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }

            return Json(response);
        }

        /*
         * Theme Configuration
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult Theme()
        {
            return View(GetSetting);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Theme([Required] string ThemeColor, [Required] string ThemeSidebar, [Required] string ThemeNavbar)
        {
            var response = new Dictionary<string, string>();

            if (GetSetting != null && ModelState.IsValid)
            {
                GetSetting.ThemeColor = ThemeColor;
                GetSetting.ThemeNavbar = ThemeNavbar;
                GetSetting.ThemeSidebar = ThemeSidebar;

                try
                {
                    _dbContext.AppSettings.Update(GetSetting);
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving email configuration.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }

            return Json(response);
        }

        /*
         * Language
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Languages()
        {
            var lang = await _dbContext.Languages.ToListAsync();
            return View(lang);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EditLanguage(int? Id)
        {
            var lang = await _dbContext.StringResources.Where(x => x.LanguageId == Id).OrderBy(x => x.Name).ToListAsync();

            if (lang != null)
            {
                var name = _dbContext.Languages.FirstOrDefault(x => x.Id == Id);
                ViewBag.name = name.Name;
                return View(lang);
            }

            return NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateLanguage([Required] int Key, string Value)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var item = _dbContext.StringResources.FirstOrDefault(x => x.Id == Key);
                    item.Value = Value.Trim();
                    _dbContext.StringResources.Update(item);
                    await _dbContext.SaveChangesAsync();
                    return Json(new
                    {
                        status = "success",
                        message = "success"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Something went wrong."
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

        /*
         * Manage Taxes
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> TaxRates()
        {
            var taxes = await _dbContext.TaxRates.ToListAsync();
            return View(taxes);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveTaxRate(TaxRate tax)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            tax.IsDefault = false;
            if (ModelState.IsValid)
            {
                tax.UpdatedAt = CurrentDateTime();
                tax.UpdatedBy = userName;

                if (tax.Id > 0)
                {
                    var existing = _dbContext.TaxRates.AsNoTracking().FirstOrDefault(x => x.Id == tax.Id);
                    tax.IsDefault = existing.IsDefault;
                    _dbContext.TaxRates.Update(tax);
                }

                if (tax.Id > 0)
                    _dbContext.TaxRates.Update(tax);
                else
                    _dbContext.TaxRates.Add(tax);
                try
                {
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving tax rate.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }
            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult TaxRateDetail(int? Id)
        {
            try
            {
                var row = _dbContext.TaxRates.FirstOrDefault(x => x.Id == Id);
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
                        message = "Tax Rate not exist."
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
        public async Task<JsonResult> DeleteTaxRate(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.TaxRates.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    if (existing.IsDefault)
                    {
                        response.Add("status", "error");
                        response.Add("message", "You can't delete default tax rate.");
                    }
                    else
                    {
                        _dbContext.TaxRates.Remove(existing);
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
                    response.Add("message", "Error while deleting tax rate.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Tax rate not exist.");
            }
            return Json(response);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> MarkDefaultTaxRate(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.TaxRates.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    existing.IsDefault = true;
                    _dbContext.TaxRates.Update(existing);

                    foreach (var item in _dbContext.TaxRates.Where(x => x.Id != existing.Id))
                    {
                        item.IsDefault = false;
                        _dbContext.TaxRates.Update(item);
                    }
                    _dbContext.SaveChanges();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while marking default tax rate.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Tax rate not exist.");
            }
            return Json(response);
        }

        /*
         * Manage Discounts
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Discounts()
        {
            var discounts = await _dbContext.Discounts.ToListAsync();
            return View(discounts);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveDiscount(Discount discount)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            discount.IsDefault = false;
            if (ModelState.IsValid)
            {
                discount.UpdatedAt = CurrentDateTime();
                discount.UpdatedBy = userName;

                if (discount.Id > 0)
                {
                    var existing = _dbContext.Discounts.AsNoTracking().FirstOrDefault(x => x.Id == discount.Id);
                    discount.IsDefault = existing.IsDefault;
                    _dbContext.Discounts.Update(discount);
                }
                else
                    _dbContext.Discounts.Add(discount);
                try
                {
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving discount.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }
            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult DiscountDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Discounts.FirstOrDefault(x => x.Id == Id);
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
                        message = "Discount not exist."
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
        public async Task<JsonResult> DeleteDiscount(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Discounts.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    if (existing.IsDefault)
                    {
                        response.Add("status", "error");
                        response.Add("message", "You can't delete default discount.");
                    }
                    else
                    {
                        _dbContext.Discounts.Remove(existing);
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
                    response.Add("message", "Error while deleting discount.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Discount not exist.");
            }

            return Json(response);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> MarkDefaultDiscount(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Discounts.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    existing.IsDefault = true;
                    _dbContext.Discounts.Update(existing);

                    foreach (var item in _dbContext.Discounts.Where(x => x.Id != existing.Id))
                    {
                        item.IsDefault = false;
                        _dbContext.Discounts.Update(item);
                    }
                    _dbContext.SaveChanges();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while marking default discount.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Discount not exist.");
            }
            return Json(response);
        }

        /*
         * Manage Charges
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Charges()
        {
            var charges = await _dbContext.Charges.ToListAsync();
            return View(charges);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveCharge(Charge charge)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            charge.IsDefault = false;
            if (ModelState.IsValid)
            {
                charge.UpdatedAt = CurrentDateTime();
                charge.UpdatedBy = userName;

                if (charge.Id > 0)
                {
                    var existing = _dbContext.Charges.AsNoTracking().FirstOrDefault(x => x.Id == charge.Id);
                    charge.IsDefault = existing.IsDefault;
                    _dbContext.Charges.Update(charge);
                }
                else
                    _dbContext.Charges.Add(charge);

                try
                {
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "success");
                    response.Add("message", "Error while saving charges.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }
            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult ChargeDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Charges.FirstOrDefault(x => x.Id == Id);
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
                        message = "Charge not exist."
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
        public async Task<JsonResult> DeleteCharge(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Charges.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    if (existing.IsDefault)
                    {
                        response.Add("status", "error");
                        response.Add("message", "You can't delete default charge.");
                    }
                    else
                    {
                        _dbContext.Charges.Remove(existing);
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
                    response.Add("message", "Error while deleting charges.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Charge not exist.");
            }

            return Json(response);
        }
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> MarkDefaultCharge(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Charges.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    existing.IsDefault = true;
                    _dbContext.Charges.Update(existing);

                    foreach (var item in _dbContext.Charges.Where(x => x.Id != existing.Id))
                    {
                        item.IsDefault = false;
                        _dbContext.Charges.Update(item);
                    }
                    _dbContext.SaveChanges();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while marking default charge.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Charge not exist.");
            }
            return Json(response);
        }
        /*
         * Manage Payment Method
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PaymentMethods()
        {
            var payment = await _dbContext.PaymentMethods.ToListAsync();
            return View(payment);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SavePaymentMethod(PaymentMethod payment)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                payment.UpdatedAt = CurrentDateTime();
                payment.UpdatedBy = userName;

                if (payment.Id > 0)
                    _dbContext.PaymentMethods.Update(payment);
                else
                    _dbContext.PaymentMethods.Add(payment);

                try
                {
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "success");
                    response.Add("message", "Error while saving payment method.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }
            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult PaymentMethodDetail(int? Id)
        {
            try
            {
                var row = _dbContext.PaymentMethods.FirstOrDefault(x => x.Id == Id);
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
                        message = "Payment method not exist."
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
        public async Task<JsonResult> DeletePaymentMethod(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.PaymentMethods.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    _dbContext.PaymentMethods.Remove(existing);
                    await _dbContext.SaveChangesAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while deleting payment method.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Payment method not exist.");
            }

            return Json(response);
        }

        /*
         * Email Templates
         */
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EmailTemplates()
        {
            var payment = await _dbContext.EmailTemplates.ToListAsync();
            return View(payment);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveEmailTemplate(EmailTemplate emailtemplate)
        {
            var response = new Dictionary<string, string>();

            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                var existing = _dbContext.EmailTemplates.FirstOrDefault(x => x.Id == emailtemplate.Id);
                if (existing != null)
                {
                    try
                    {
                        existing.UpdatedBy = userName;
                        existing.UpdatedAt = CurrentDateTime();
                        existing.Template = emailtemplate.Template;
                        existing.Subject = emailtemplate.Subject;
                        _dbContext.EmailTemplates.Update(existing);
                        await _dbContext.SaveChangesAsync();

                        response.Add("status", "success");
                        response.Add("message", "success");
                    }
                    catch
                    {
                        response.Add("status", "success");
                        response.Add("message", "Error while saving email template.");
                    }
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Email Template not exist.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Please enter required fields.");
            }
            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult EmailTemplateDetail(int? Id)
        {
            try
            {
                var row = _dbContext.EmailTemplates.FirstOrDefault(x => x.Id == Id);
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
                        message = "Email Template not exist."
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

        [HttpPut]
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> RestoreEmailTemplate(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.EmailTemplates.FindAsync(Id);
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (existing != null)
            {
                try
                {
                    existing.UpdatedBy = userName;
                    existing.UpdatedAt = CurrentDateTime();
                    existing.Template = existing.DefaultTemplate;
                    _dbContext.EmailTemplates.Update(existing);
                    await _dbContext.SaveChangesAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while restoring email template.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Email Template not exist.");
            }

            return Json(response);
        }

    }
}