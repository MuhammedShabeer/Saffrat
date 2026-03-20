using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Helpers;
using Saffrat.Models;
using Saffrat.Services;
using Saffrat.Services.AccountingEngine;
using Saffrat.Models.AccountingEngine;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace Saffrat.Controllers
{
    public class HRMController : BaseController
    {
        private readonly ILogger<HRMController> _logger;
        private readonly RestaurantDBContext _dbContext;
        private readonly IAccountingEngine _accountingEngine;


        public HRMController(ILogger<HRMController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService,
            IAccountingEngine accountingEngine)
        : base(languageService, localizationService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _accountingEngine = accountingEngine;
        }

        /*
         * Department Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Departments()
        {
            var departments = await _dbContext.Departments.ToListAsync();
            return View(departments);
        }

        /*
         * Department APIs
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public JsonResult DepartmentDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Departments.FirstOrDefault(x => x.Id == Id);
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
                        message = "Department not exist."
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
        public async Task<IActionResult> SaveDepartment(Department department)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    department.UpdatedAt = CurrentDateTime();
                    department.UpdatedBy = userName;
                    if (department.Id > 0)
                    {
                        _dbContext.Departments.Update(department);
                        _dbContext.SaveChanges();
                    }
                    else
                    {
                        _dbContext.Departments.Add(department);
                        await _dbContext.SaveChangesAsync();
                    }

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving department.");
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
        public async Task<JsonResult> DeleteDepartment(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.Departments.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.Departments.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch (DbUpdateException)
                {
                    results.Add("status", "error");
                    results.Add("message", "Your attempt to delete record could not be completed because it is associated with other table.");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting department.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Department not exist.");
            }

            return Json(results);
        }

        /*
         * Designation Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Designations()
        {
            var designations = await _dbContext.Designations.ToListAsync();
            return View(designations);
        }

        /*
         * Designation APIs
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public JsonResult DesignationDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Designations.FirstOrDefault(x => x.Id == Id);
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
                        message = "Designation not exist."
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
        public async Task<IActionResult> SaveDesignation(Designation designation)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    designation.UpdatedAt = CurrentDateTime();
                    designation.UpdatedBy = userName;
                    if (designation.Id > 0)
                    {
                        _dbContext.Designations.Update(designation);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        _dbContext.Designations.Add(designation);
                        await _dbContext.SaveChangesAsync();
                    }

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving designation.");
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
        public async Task<JsonResult> DeleteDesignation(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.Designations.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.Designations.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch (DbUpdateException)
                {
                    results.Add("status", "error");
                    results.Add("message", "Your attempt to delete record could not be completed because it is associated with other table.");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting designation.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Designation not exist.");
            }

            return Json(results);
        }

        /*
         * Office Shift Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> OfficeShift()
        {
            var shifts = await _dbContext.Shifts.ToListAsync();
            return View(shifts);
        }

        /*
         * Office Shift APIs
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public JsonResult ShiftDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Shifts.FirstOrDefault(x => x.Id == Id);
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
                        message = "Shift not exist."
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
        public async Task<IActionResult> SaveShift(Shift shift)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    shift.UpdatedAt = CurrentDateTime();
                    shift.UpdatedBy = userName;
                    if (shift.Id > 0)
                    {
                        _dbContext.Shifts.Update(shift);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        _dbContext.Shifts.Add(shift);
                        await _dbContext.SaveChangesAsync();
                    }

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving shift.");
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
        public async Task<JsonResult> DeleteShift(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.Shifts.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.Shifts.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch (DbUpdateException)
                {
                    results.Add("status", "error");
                    results.Add("message", "Your attempt to delete record could not be completed because it is associated with other table.");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting shift.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Shift not exist.");
            }

            return Json(results);
        }

        /*
         * Employee Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Employees()
        {
            var employees = await _dbContext.Employees
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .Include(x => x.Shift)
                .Include(x => x.EmployeeEarnings)
                .Include(x => x.EmployeeDeductions).ToListAsync();
            return View(employees);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult AddEmployee()
        {
            ViewBag.Departments = GetDepartments();
            ViewBag.Designations = GetDesignatons();
            ViewBag.Shifts = GetShifts();
            return View();
        }
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult EditEmployee(int? id)
        {
            var employee = _dbContext.Employees.Where(x => x.Id == id)
                .Include(x => x.EmployeeEarnings)
                .Include(x => x.EmployeeDeductions)
                .Include(x => x.EmployeeAttachments)
                .FirstOrDefault();

            if (employee != null)
            {
                ViewBag.Departments = GetDepartments();
                ViewBag.Designations = GetDesignatons();
                ViewBag.Shifts = GetShifts();
                return View(employee);
            }
            return NotFound();
        }

        /*
         * Employee APIs
         */
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddEmployee(Employee employee, IFormFile EmployeeImage)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    employee.Image = "default";
                    if (EmployeeImage != null)
                    {
                        var res = Uploader.UploadImageMedia(1, "Employee", EmployeeImage);
                        if (res["status"] == "success")
                        {
                            employee.Image = res["message"];
                        }
                        else
                        {
                            return Json(res);
                        }
                    }
                    employee.UpdatedAt = CurrentDateTime();
                    employee.UpdatedBy = userName;
                    _dbContext.Employees.Add(employee);
                    await _dbContext.SaveChangesAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving employee.");
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
        public async Task<IActionResult> UpdateEmployee(Employee employee, IFormFile EmployeeImage)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    var existing = _dbContext.Employees.AsNoTracking().FirstOrDefault(x => x.Id == employee.Id);
                    if (EmployeeImage != null)
                    {
                        var res = Uploader.UploadImageMedia(1, "Employee", EmployeeImage);
                        if (res["status"] == "success")
                        {
                            employee.Image = res["message"];
                        }
                        else
                        {
                            return Json(res);
                        }
                    }
                    employee.Image = EmployeeImage == null ? existing.Image : employee.Image;
                    employee.UpdatedAt = CurrentDateTime();
                    employee.UpdatedBy = userName;
                    _dbContext.Employees.Update(employee);
                    await _dbContext.SaveChangesAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving employee.");
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
        public async Task<JsonResult> DeleteEmployee(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.Employees.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.Employees.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting employee.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Employee not exist.");
            }

            return Json(results);
        }

        /*
         * Employee Attachment APIs
         */
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadAttachment([Required] int EmployeeId, [Required] IFormFile attachment)
        {
            var response = new Dictionary<string, string>();
            if (ModelState.IsValid)
            {
                try
                {
                    var empAttach = new EmployeeAttachment();
                    var res = Uploader.UploadDocumentMedia(1, "Employee", attachment);
                    if (res["status"] == "success")
                    {
                        empAttach.AttachmentName = res["message"];
                    }
                    else
                    {
                        return Json(res);
                    }

                    empAttach.EmployeeId = EmployeeId;
                    empAttach.AttachmentType = "-";
                    _dbContext.EmployeeAttachments.Add(empAttach);
                    await _dbContext.SaveChangesAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                    response.Add("name", empAttach.AttachmentName);
                    response.Add("id", empAttach.Id.ToString());
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while uploading attachment.");
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
        public async Task<JsonResult> DeleteAttachment(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.EmployeeAttachments.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.EmployeeAttachments.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting attachment.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Attachment not exist.");
            }

            return Json(results);
        }

        /*
         * Employee Earning APIs
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult EarningDetail(int? Id)
        {
            try
            {
                var row = _dbContext.EmployeeEarnings.FirstOrDefault(x => x.Id == Id);
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
                        message = "Earning not exist."
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
        public async Task<IActionResult> SaveEarning(EmployeeEarning income)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    income.UpdatedAt = CurrentDateTime();
                    income.UpdatedBy = userName;
                    if (income.Id > 0)
                    {
                        _dbContext.EmployeeEarnings.Update(income);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        _dbContext.EmployeeEarnings.Add(income);
                        await _dbContext.SaveChangesAsync();
                    }

                    response.Add("id", income.Id.ToString());
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving earning.");
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
        public async Task<JsonResult> DeleteEarning(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.EmployeeEarnings.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.EmployeeEarnings.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting earning.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Earning not exist.");
            }

            return Json(results);
        }

        /*
         * Employee Deduction APIs
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult DeductionDetail(int? Id)
        {
            try
            {
                var row = _dbContext.EmployeeDeductions.FirstOrDefault(x => x.Id == Id);
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
                        message = "Deduction not exist."
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
        public async Task<IActionResult> SaveDeduction(EmployeeDeduction deduction)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    deduction.UpdatedAt = CurrentDateTime();
                    deduction.UpdatedBy = userName;
                    if (deduction.Id > 0)
                    {
                        _dbContext.EmployeeDeductions.Update(deduction);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        _dbContext.EmployeeDeductions.Add(deduction);
                        await _dbContext.SaveChangesAsync();
                    }

                    response.Add("id", deduction.Id.ToString());
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving deduction.");
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
        public async Task<JsonResult> DeleteDeduction(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.EmployeeDeductions.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.EmployeeDeductions.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting deduction.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Deduction not exist.");
            }

            return Json(results);
        }

        /*
         * Payroll Views
         */
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Payroll(int? month, int? year)
        {
            var current = CurrentDateTime();
            if (month > 0 && year > 0)
            {
                var payroll = await _dbContext.Payrolls.Where(x => x.Month == month && x.Year == year)
                    .Include(x => x.PayrollDetails)
                    .Include(x => x.Employee)
                    .ThenInclude(x => x.Department)
                    .Include(x => x.Employee)
                    .ThenInclude(x => x.Designation).ToListAsync();
                ViewBag.month = month;
                ViewBag.year = year;
                return View(payroll);
            }
            else
            {
                var payroll = await _dbContext.Payrolls.Where(x => x.Month == current.Month && x.Year == current.Year)
                    .Include(x => x.PayrollDetails)
                    .Include(x => x.Employee)
                    .ThenInclude(x => x.Department)
                    .Include(x => x.Employee)
                    .ThenInclude(x => x.Designation).ToListAsync();
                ViewBag.month = current.Month;
                ViewBag.year = current.Year;
                return View(payroll);
            }
        }

        // Get employees for customizable payroll generation
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetEmployeesForPayroll(int month, int year)
        {
            try
            {
                // Get IDs of employees who already have payroll for this month/year
                var generatedEmployeeIds = await _dbContext.Payrolls
                    .Where(p => p.Month == month && p.Year == year)
                    .Select(p => (int?)p.EmployeeId)
                    .ToListAsync();

                var employees = await _dbContext.Employees
                    .Where(x => x.Status == true && !generatedEmployeeIds.Contains(x.Id))
                    .Select(e => new
                    {
                        e.Id,
                        e.Name,
                        e.Salary,
                        DepartmentName = e.Department.Title,
                        DesignationName = e.Designation.Title
                    })
                    .ToListAsync();

                return Json(new { status = "success", data = employees });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GeneratePayroll([Required] int month, [Required] int year)
        {
            if (ModelState.IsValid)
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var current = CurrentDateTime();
                using var transaction = _dbContext.Database.BeginTransaction();
                try
                {
                    var employees = await _dbContext.Employees.Where(x => x.Status.Equals(true))
                    .Include(x => x.EmployeeEarnings)
                    .Include(x => x.EmployeeDeductions)
                    .ToListAsync();

                    foreach (var item in employees)
                    {
                        var existing = _dbContext.Payrolls.Where(x => x.EmployeeId == item.Id && x.Month == month && x.Year == year).FirstOrDefault();
                        if (existing != null)
                        {
                            var existingJournals = _dbContext.JournalEntries.Where(x => x.SourceDocumentType == "payroll" && x.SourceDocumentId == existing.Id).ToList();
                            foreach (var journal in existingJournals)
                            {
                                await _accountingEngine.ReverseJournalEntryAsync(journal.Id);
                            }
                            _dbContext.Payrolls.Remove(existing);
                            await _dbContext.SaveChangesAsync();
                        }

                        var payroll = new Payroll()
                        {
                            EmployeeId = Convert.ToInt32(item.Id),
                            Month = month,
                            Year = year,
                            GeneratedBy = userName,
                            GeneratedAt = current,
                            PayrollType = item.PayslipType,
                            Salary = item.Salary,
                            NetSalary = 0,
                            PaymentStatus = "Unpaid",
                            AdvanceAmountPaid = 0,
                            TotalAmountPaid = 0,
                            RemainingBalance = 0
                        };
                        _dbContext.Payrolls.Add(payroll);
                        await _dbContext.SaveChangesAsync();
                        decimal netsalary = item.Salary;
                        foreach (var income in item.EmployeeEarnings)
                        {
                            if (income.IsPercentage)
                            {
                                netsalary += (income.Amount * payroll.Salary) / 100;
                            }
                            else
                            {
                                netsalary += income.Amount;
                            }

                            var payrollDetail = new PayrollDetail()
                            {
                                Title = income.Title,
                                PayrollId = payroll.Id,
                                IsPercentage = income.IsPercentage,
                                AmountType = "Earning",
                                Amount = income.Amount
                            };
                            _dbContext.PayrollDetails.Add(payrollDetail);
                        }
                        foreach (var deduction in item.EmployeeDeductions)
                        {
                            if (deduction.IsPercentage)
                            {
                                netsalary -= (deduction.Amount * payroll.Salary) / 100;
                            }
                            else
                            {
                                netsalary -= deduction.Amount;
                            }

                            var payrollDetail = new PayrollDetail()
                            {
                                Title = deduction.Title,
                                PayrollId = payroll.Id,
                                IsPercentage = deduction.IsPercentage,
                                AmountType = "Deduction",
                                Amount = deduction.Amount
                            };
                            _dbContext.PayrollDetails.Add(payrollDetail);
                        }
                        payroll.NetSalary = netsalary;
                        payroll.RemainingBalance = netsalary;
                        _dbContext.Payrolls.Update(payroll);
                        _dbContext.SaveChanges();
                    }
                    await transaction.CommitAsync();
                    return Json(new
                    {
                        status = "success",
                        message = "success"
                    });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return Json(new
                    {
                        status = "error",
                        message = "Error while generating payroll."
                    });
                }
            }
            else
            {
                return Json(new
                {
                    status = "error",
                    message = "Enter required fields."
                });
            }
        }

        // Generate payroll with custom salary amounts per employee
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GenerateCustomPayroll([Required] int month, [Required] int year, [FromBody] List<PayrollCustomData> customData)
        {
            if (customData == null || customData.Count == 0)
            {
                return Json(new { status = "error", message = "No payroll data provided." });
            }

            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var current = CurrentDateTime();
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                foreach (var data in customData)
                {
                    var employee = await _dbContext.Employees
                        .Include(x => x.EmployeeEarnings)
                        .Include(x => x.EmployeeDeductions)
                        .FirstOrDefaultAsync(x => x.Id == data.EmployeeId);

                    if (employee == null) continue;

                    // Remove existing payroll for this month/year if any
                    var existing = _dbContext.Payrolls.FirstOrDefault(x => 
                        x.EmployeeId == data.EmployeeId && 
                        x.Month == month && 
                        x.Year == year);

                    if (existing != null)
                    {
                        var existingJournals = _dbContext.JournalEntries
                            .Where(x => x.SourceDocumentType == "payroll" && x.SourceDocumentId == existing.Id)
                            .ToList();
                        foreach (var journal in existingJournals)
                        {
                            await _accountingEngine.ReverseJournalEntryAsync(journal.Id);
                        }
                        _dbContext.Payrolls.Remove(existing);
                        await _dbContext.SaveChangesAsync();
                    }

                    // Create payroll with custom salary amount
                    var payroll = new Payroll()
                    {
                        EmployeeId = data.EmployeeId,
                        Month = month,
                        Year = year,
                        GeneratedBy = userName,
                        GeneratedAt = current,
                        PayrollType = employee.PayslipType,
                        Salary = data.CustomSalary,  // Use custom salary instead of default
                        NetSalary = 0,
                        PaymentStatus = "Unpaid",
                        AdvanceAmountPaid = 0,
                        TotalAmountPaid = data.InitialPayingAmount,  // Set initial paying amount if provided
                        RemainingBalance = data.CustomSalary
                    };
                    _dbContext.Payrolls.Add(payroll);
                    await _dbContext.SaveChangesAsync();

                    // Calculate Net Salary with earnings and deductions
                    decimal netsalary = data.CustomSalary;

                    foreach (var income in employee.EmployeeEarnings)
                    {
                        decimal earningAmount;
                        if (income.IsPercentage)
                        {
                            earningAmount = (income.Amount * data.CustomSalary) / 100;
                            netsalary += earningAmount;
                        }
                        else
                        {
                            earningAmount = income.Amount;
                            netsalary += earningAmount;
                        }

                        var payrollDetail = new PayrollDetail()
                        {
                            Title = income.Title,
                            PayrollId = payroll.Id,
                            IsPercentage = income.IsPercentage,
                            AmountType = "Earning",
                            Amount = income.Amount
                        };
                        _dbContext.PayrollDetails.Add(payrollDetail);
                    }

                    foreach (var deduction in employee.EmployeeDeductions)
                    {
                        decimal deductionAmount;
                        if (deduction.IsPercentage)
                        {
                            deductionAmount = (deduction.Amount * data.CustomSalary) / 100;
                            netsalary -= deductionAmount;
                        }
                        else
                        {
                            deductionAmount = deduction.Amount;
                            netsalary -= deductionAmount;
                        }

                        var payrollDetail = new PayrollDetail()
                        {
                            Title = deduction.Title,
                            PayrollId = payroll.Id,
                            IsPercentage = deduction.IsPercentage,
                            AmountType = "Deduction",
                            Amount = deduction.Amount
                        };
                        _dbContext.PayrollDetails.Add(payrollDetail);
                    }

                    payroll.NetSalary = netsalary;

                    // Validate and set remaining balance
                    if (data.InitialPayingAmount > netsalary)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { status = "error", message = $"Initial payment amount cannot exceed net salary of {netsalary:C}" });
                    }

                    payroll.RemainingBalance = netsalary - data.InitialPayingAmount;

                    // Update payment status based on initial payment amount
                    if (data.InitialPayingAmount > 0)
                    {
                        if (data.InitialPayingAmount >= netsalary)
                        {
                            payroll.PaymentStatus = "Paid";
                        }
                        else
                        {
                            payroll.PaymentStatus = "PartiallyPaid";
                        }

                        // NEW: Create Journal Entry for Initial Payment
                        int salaryAccountId = 0;
                        var salaryAccount = await _dbContext.GLAccounts.FirstOrDefaultAsync(x => x.AccountName == "Salaries" || (x.Category == 4 && x.Type == 15));
                        if (salaryAccount != null)
                        {
                            salaryAccountId = salaryAccount.Id;
                        }
                        else
                        {
                            var newExpAccount = new GLAccount
                            {
                                AccountCode = "5000",
                                AccountName = "Salaries",
                                Category = 4,
                                Type = 15,
                                CurrentBalance = 0,
                                IsActive = true
                            };
                            _dbContext.GLAccounts.Add(newExpAccount);
                            await _dbContext.SaveChangesAsync();
                            salaryAccountId = newExpAccount.Id;
                        }

                        var initialPaymentJournal = new JournalEntry
                        {
                            ReferenceNumber = "PAYROLL-" + payroll.Id,
                            Description = $"Initial Salary Payment - {payroll.Month}/{payroll.Year}",
                            EntryDate = CurrentDateTime(),
                            IsPosted = false,
                            SourceDocumentType = "payroll",
                            SourceDocumentId = payroll.Id,
                            CreatedAt = CurrentDateTime(),
                            LedgerEntries = new List<LedgerEntry>()
                        };

                        initialPaymentJournal.LedgerEntries.Add(new LedgerEntry
                        {
                            GLAccountId = salaryAccountId,
                            Description = "Initial Salary Payment",
                            Debit = data.InitialPayingAmount,
                            Credit = 0
                        });

                        initialPaymentJournal.LedgerEntries.Add(new LedgerEntry
                        {
                            GLAccountId = Convert.ToInt32(GetSetting.PayrollAccount),
                            Description = "Initial Salary Payment",
                            Debit = 0,
                            Credit = data.InitialPayingAmount
                        });

                        await _accountingEngine.PostJournalEntryAsync(initialPaymentJournal);

                        // NEW: Create PayrollPayment record for history
                        var payment = new PayrollPayment
                        {
                            PayrollId = payroll.Id,
                            Amount = data.InitialPayingAmount,
                            PaymentMethod = "Cash", // Default for initial payment
                            PaymentDate = CurrentDateTime(),
                            Notes = "Initial payment during generation",
                            JournalEntryId = initialPaymentJournal.Id,
                            CreatedAt = CurrentDateTime()
                        };
                        _dbContext.PayrollPayments.Add(payment);
                        
                        payroll.TotalAmountPaid = data.InitialPayingAmount;
                    }

                    _dbContext.Payrolls.Update(payroll);
                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Json(new { status = "success", message = "Payroll generated successfully with custom amounts." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { status = "error", message = $"Error: {ex.Message}" });
            }
        }

        [HttpPut]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PaySalary(int? Id)
        {
            try
            {
                var payroll = await _dbContext.Payrolls.FirstOrDefaultAsync(x => x.Id == Id);
                if (payroll != null)
                {
                    if (payroll.PaymentStatus == "Paid")
                    {
                        return Json(new { status = "error", message = "Payroll already paid." });
                    }

                    decimal amountToPay = payroll.RemainingBalance;
                    if (amountToPay <= 0)
                    {
                        payroll.PaymentStatus = "Paid";
                        _dbContext.Payrolls.Update(payroll);
                        await _dbContext.SaveChangesAsync();
                        return Json(new { status = "success", message = "Payroll marked as paid (Zero balance)." });
                    }

                    // Double-Entry Accounting
                    int salaryAccountId = 0;
                    var salaryAccount = await _dbContext.GLAccounts.FirstOrDefaultAsync(x => x.AccountName == "Salaries" || (x.Category == 4 && x.Type == 15));
                    if (salaryAccount != null)
                    {
                        salaryAccountId = salaryAccount.Id;
                    }
                    else
                    {
                        var newExpAccount = new GLAccount
                        {
                            AccountCode = "5000",
                            AccountName = "Salaries",
                            Category = 4, // Expense
                            Type = 15,    // Payroll/Remuneration
                            CurrentBalance = 0,
                            IsActive = true
                        };
                        _dbContext.GLAccounts.Add(newExpAccount);
                        await _dbContext.SaveChangesAsync();
                        salaryAccountId = newExpAccount.Id;
                    }

                    var payrollJournal = new JournalEntry
                    {
                        ReferenceNumber = "PAYROLL-" + payroll.Id,
                        Description = $"Salary Payment - {payroll.Month}/{payroll.Year}",
                        EntryDate = CurrentDateTime(),
                        IsPosted = false, // Will be set to true by PostJournalEntryAsync
                        SourceDocumentType = "payroll",
                        SourceDocumentId = payroll.Id,
                        CreatedAt = CurrentDateTime(),
                        LedgerEntries = new List<LedgerEntry>()
                    };

                    payrollJournal.LedgerEntries.Add(new LedgerEntry
                    {
                        GLAccountId = salaryAccountId,
                        Description = "Salary Payment",
                        Debit = amountToPay,
                        Credit = 0
                    });

                    payrollJournal.LedgerEntries.Add(new LedgerEntry
                    {
                        GLAccountId = Convert.ToInt32(GetSetting.PayrollAccount),
                        Description = "Salary Payment",
                        Debit = 0,
                        Credit = amountToPay
                    });

                    // Use accounting engine to post and update balances
                    await _accountingEngine.PostJournalEntryAsync(payrollJournal);

                    // NEW: Create PayrollPayment record for history
                    var payment = new PayrollPayment
                    {
                        PayrollId = payroll.Id,
                        Amount = amountToPay,
                        PaymentMethod = "Cash", // Default for marking fully paid
                        PaymentDate = CurrentDateTime(),
                        Notes = "Marked as fully paid from list",
                        JournalEntryId = payrollJournal.Id,
                        CreatedAt = CurrentDateTime()
                    };
                    _dbContext.PayrollPayments.Add(payment);

                    payroll.TotalAmountPaid += amountToPay;
                    payroll.RemainingBalance = 0;
                    payroll.PaymentStatus = "Paid";
                    _dbContext.Payrolls.Update(payroll);
                    await _dbContext.SaveChangesAsync();

                    return Json(new { status = "success", message = "Salary paid successfully." });
                }
                else
                {
                    return Json(new { status = "error", message = "Payroll not exist." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PayrollDetail(int? Id)
        {
            try
            {
                var payroll = await _dbContext.Payrolls.Where(x => x.Id.Equals(Id))
                    .Include(x => x.PayrollDetails)
                    .Include(x => x.Employee)
                    .ThenInclude(x => x.Department)
                    .Include(x => x.Employee)
                    .ThenInclude(x => x.Designation)
                    .FirstOrDefaultAsync();
                if (payroll != null)
                {
                    return Json(new
                    {
                        data = payroll,
                        status = "success",
                        message = "success"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Payroll not exist."
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
        public async Task<JsonResult> DeletePayroll(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.Payrolls.FindAsync(Id);

            if (existing != null)
            {
                using var transaction = _dbContext.Database.BeginTransaction();
                try
                {
                    // Find and reverse associated journals
                    var existingJournals = _dbContext.JournalEntries.Where(x => x.SourceDocumentType == "payroll" && x.SourceDocumentId == existing.Id).ToList();
                    foreach (var journal in existingJournals)
                    {
                        await _accountingEngine.ReverseJournalEntryAsync(journal.Id);
                    }

                    _dbContext.Payrolls.Remove(existing);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting payroll: " + ex.Message);
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Payroll not exist.");
            }

            return Json(results);
        }

        /*
         * Attandance Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Attendance(DateTime? date, int? shift)
        {
            var start = StartOfDay(date);
            var attendances = await _dbContext.Attendances.OrderByDescending(x => x.Id)
                .Where(x => x.AttendaceDate.Date == start.Date)
                .Include(x => x.Shift)
                .Include(x => x.Employee)
                .ThenInclude(x => x.Department)
                .Include(x => x.Employee)
                .ThenInclude(x => x.Designation).ToListAsync();

            if (shift > 0)
            {
                attendances = attendances.Where(x => x.ShiftId == shift).ToList();
            }
            ViewBag.shifts = GetShifts();
            ViewBag.date = start.ToString("yyyy-MM-dd");
            ViewBag.shift = shift;

            return View(attendances);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddAttendance(int? shift, DateTime? date)
        {
            var start = StartOfDay(date);
            ViewBag.date = start.ToString("yyyy-MM-dd");
            ViewBag.shift = shift;
            ViewBag.shifts = GetShifts();
            var currentshift = _dbContext.Shifts.FirstOrDefault(x => x.Id == shift);
            if (shift != null && date != null)
            {
                try
                {
                    var employees = await _dbContext.Employees.Where(x => x.ShiftId == currentshift.Id && x.Status.Equals(true))
                    .Include(x => x.Department)
                    .Include(x => x.Designation)
                    .ToListAsync();

                    ViewBag.currentshift = currentshift;
                    return View(employees);
                }
                catch
                {
                    return View(null);
                }
            }
            return View(null);
        }

        /*
         * Attendance APIs
         */

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddAttendance([Required] DateTime date, [Required] int ShiftId, [Required] int[] EmployeeId, [Required] TimeSpan[] ClockIn, [Required] TimeSpan[] ClockOut, [Required] string[] Status, string[] Note)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var start = date;
            if (ModelState.IsValid && EmployeeId.Length == ClockIn.Length && ClockIn.Length == ClockOut.Length && ClockOut.Length == Status.Length && Status.Length == Note.Length)
            {
                using var transaction = _dbContext.Database.BeginTransaction();
                try
                {
                    for (int i = 0; i < ClockIn.Length; i++)
                    {
                        var attendance = _dbContext.Attendances.FirstOrDefault(x => x.ShiftId == ShiftId && x.AttendaceDate.Date == start.Date);
                        if (attendance == null)
                        {
                            attendance = new()
                            {
                                ShiftId = ShiftId,
                                EmployeeId = EmployeeId[i],
                                ClockIn = ClockIn[i],
                                ClockOut = ClockOut[i],
                                Status = Status[i],
                                Note = Note[i],
                                UpdatedAt = start,
                                UpdatedBy = userName,
                                AttendaceDate = start
                            };
                            _dbContext.Attendances.Add(attendance);
                        }
                        else
                        {
                            attendance.ShiftId = ShiftId;
                            attendance.EmployeeId = EmployeeId[i];
                            attendance.ClockIn = ClockIn[i];
                            attendance.ClockOut = ClockOut[i];
                            attendance.Status = Status[i];
                            attendance.Note = Note[i];
                            attendance.UpdatedAt = start;
                            attendance.UpdatedBy = userName;
                            attendance.AttendaceDate = start;
                            _dbContext.Attendances.Update(attendance);
                        }
                    }
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    response.Add("status", "error");
                    response.Add("message", "Error while saving attendance.");
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
        public JsonResult AttendanceDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Attendances.FirstOrDefault(x => x.Id == Id);
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
                        message = "Attendance not exist."
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
        public async Task<JsonResult> DeleteAttendance(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.Attendances.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.Attendances.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting attendance.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Attendance not exist.");
            }

            return Json(results);
        }

        /*
         * Leave Requests Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LeaveRequests()
        {
            var requests = await _dbContext.LeaveRequests.OrderByDescending(x => x.Id)
                .Include(x => x.Employee).ToListAsync();
            ViewBag.employees = GetEmployees();
            return View(requests);
        }

        /*
         * Leave Requests APIs
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public JsonResult LeaveRequestDetail(int? Id)
        {
            try
            {
                var row = _dbContext.LeaveRequests.FirstOrDefault(x => x.Id == Id);
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
                        message = "Leave Request not exist."
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
        public async Task<IActionResult> SaveLeaveRequest(LeaveRequest request)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    request.EndDate = EndOfDay(request.EndDate);
                    request.StartDate = StartOfDay(request.StartDate);
                    request.UpdatedAt = CurrentDateTime();
                    request.UpdatedBy = userName;
                    request.Days = Convert.ToInt32((request.EndDate - request.StartDate).TotalDays);
                    if (request.Id > 0)
                    {
                        _dbContext.LeaveRequests.Update(request);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        _dbContext.LeaveRequests.Add(request);
                        await _dbContext.SaveChangesAsync();
                    }

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving leave request.");
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
        public async Task<JsonResult> DeleteLeaveRequest(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.LeaveRequests.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.LeaveRequests.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting leave request.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Leave Request not exist.");
            }

            return Json(results);
        }


        /*
         * Holidays Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Holidays()
        {
            var holidays = await _dbContext.Holidays.ToListAsync();
            return View(holidays);
        }

        /*
         * Holiday APIs
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public JsonResult HolidayDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Holidays.FirstOrDefault(x => x.Id == Id);
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
                        message = "Holiday not exist."
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
        public async Task<IActionResult> SaveHoliday(Holiday holiday)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    holiday.UpdatedAt = CurrentDateTime();
                    holiday.UpdatedBy = userName;
                    if (holiday.Id > 0)
                    {
                        _dbContext.Holidays.Update(holiday);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        _dbContext.Holidays.Add(holiday);
                        await _dbContext.SaveChangesAsync();
                    }

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving holiday.");
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
        public async Task<JsonResult> DeleteHoliday(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.Holidays.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.Holidays.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting holiday.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Holiday not exist.");
            }

            return Json(results);
        }

        /*
         * Private Functions
         */
        private Dictionary<int, string> GetDesignatons()
        {
            Dictionary<int, string> designations = _dbContext.Designations
                .Select(t => new
                {
                    t.Id,
                    t.Title
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.Title);
            return designations;
        }
        private Dictionary<int, string> GetDepartments()
        {
            Dictionary<int, string> accounts = _dbContext.Departments
                .Select(t => new
                {
                    t.Id,
                    t.Title
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.Title);
            return accounts;
        }
        private Dictionary<int, string> GetShifts()
        {
            Dictionary<int, string> shifts = _dbContext.Shifts
                .Select(t => new
                {
                    t.Id,
                    t.Title
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.Title);
            return shifts;
        }
        private Dictionary<int, string> GetEmployees()
        {
            Dictionary<int, string> employees = _dbContext.Employees.Where(x => x.Status.Equals(true))
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => String.Format("{0} (EMP-{1})", t.Name, t.Id));
            return employees;
        }
        private Dictionary<int, string> GetPaymentMethods()
        {
            Dictionary<int, string> methods = _dbContext.PaymentMethods
                .Select(t => new
                {
                    t.Id,
                    t.Title
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.Title);
            return methods;
        }
    }
}