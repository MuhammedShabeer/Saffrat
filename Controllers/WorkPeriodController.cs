using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Saffrat.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    // Controller for managing work periods
    public class WorkPeriodController : BaseController
    {
        private readonly ILogger<WorkPeriodController> _logger;
        private readonly RestaurantDBContext _dbContext;

        // Constructor for WorkPeriodController
        public WorkPeriodController(
            ILogger<WorkPeriodController> logger,
            RestaurantDBContext dbContext,
            ILanguageService languageService,
            ILocalizationService localizationService)
            : base(languageService, localizationService)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        // Action for displaying work period information
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult Index()
        {
            var workPeriod = _dbContext.WorkPeriods.OrderByDescending(x => x.Id).FirstOrDefault();
            return View(workPeriod);
        }

        // Action to start a new work period
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> StartWorkPeriod([Required] decimal OpeningBalance)
        {
            var response = new Dictionary<string, string>();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (ModelState.IsValid)
                {
                    if (!IsWorkPeriodStarted())
                    {
                        // Create a new work period
                        WorkPeriod workPeriod = new()
                        {
                            OpeningBalance = OpeningBalance,
                            StartedBy = userName,
                            ClosingBalance = 0,
                            EndBy = userName,
                            IsEnd = false,
                            StartedAt = CurrentDateTime(),
                            EndAt = CurrentDateTime()
                        };

                        _dbContext.WorkPeriods.Add(workPeriod);
                        await _dbContext.SaveChangesAsync();

                        response.Add("status", "success");
                        response.Add("message", "success");
                    }
                    else
                    {
                        response.Add("status", "error");
                        response.Add("message", "Work Period already started.");
                    }
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Enter required fields.");
                }
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        // Action to end the current work period
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> EndWorkPeriod()
        {
            var response = new Dictionary<string, string>();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var workPeriod = _dbContext.WorkPeriods.FirstOrDefault(x => x.IsEnd.Equals(false));

                if (workPeriod != null)
                {
                    // Check if there are opened orders before ending the work period
                    var trans = _dbContext.Transactions.Where(x => x.Date >= workPeriod.StartedAt).ToList();
                    var openedOrders = _dbContext.RunningOrders.Where(x => x.Status > 0 && x.Status < 5).Count();

                    if (openedOrders > 0)
                    {
                        response.Add("status", "error");
                        response.Add("message", "You can't end work period because you need to settle all opened orders.");
                    }
                    else
                    {
                        // Calculate closing balance and end the work period
                        foreach (var item in trans)
                        {
                            workPeriod.ClosingBalance += (item.Credit - item.Debit);
                        }
                        workPeriod.ClosingBalance += workPeriod.OpeningBalance;
                        workPeriod.IsEnd = true;
                        workPeriod.EndAt = CurrentDateTime();
                        workPeriod.EndBy = userName;

                        _dbContext.WorkPeriods.Update(workPeriod);
                        await _dbContext.SaveChangesAsync();

                        response.Add("status", "success");
                        response.Add("message", "success");
                    }
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Enter required fields.");
                }
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        // Check if a work period has already started
        private bool IsWorkPeriodStarted()
        {
            var workPeriod = _dbContext.WorkPeriods.Where(x => x.IsEnd.Equals(false)).Count();
            return workPeriod > 0;
        }
    }
}
