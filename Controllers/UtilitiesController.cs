using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Saffrat.Models;
using Microsoft.EntityFrameworkCore;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class UtilitiesController : BaseController
    {
        private readonly ILogger<UtilitiesController> _logger;
        private readonly IConfiguration _configuration;
        private readonly RestaurantDBContext _dbContext;

        public UtilitiesController(ILogger<UtilitiesController> logger, RestaurantDBContext dbContext,
            IConfiguration configuration,
            ILanguageService languageService, ILocalizationService localizationService)
        : base(languageService, localizationService)
        {
            _logger = logger;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        /*
         * Audit Log
         */
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AuditLog(DateTime? from, DateTime? to)
        {
            var start = StartOfDay(from);
            var end = EndOfDay(to);
            var logs = await _dbContext.AuditLogs.OrderByDescending(x => x.Id)
                .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
                .ToListAsync();

            ViewBag.start = start.ToString("yyyy-MM-dd");
            ViewBag.end = end.ToString("yyyy-MM-dd");

            return View(logs);
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteAuditLog()
        {
            var response = new Dictionary<string, string>();
            try
            {
                _dbContext.Database.ExecuteSqlRaw("TRUNCATE TABLE [AuditLogs]");
                await _dbContext.SaveChangesAsync();

                response.Add("status", "success");
                response.Add("message", "success");
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Error while deleting audit log.");
            }

            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult Backup()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult BackupDatabase()
        {
            var response = new Dictionary<string, string>();

            try
            {
                var conn = _configuration.GetConnectionString("default");

                // Backup destination
                string path = Path.Combine(Directory.GetCurrentDirectory(), "AppFiles/Company/1/Backup");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                Server _server = new Server(new ServerConnection(new SqlConnection(conn)));

                foreach (Database db in _server.Databases)
                {
                    if(conn.Contains(db.Name))
                    {
                        string filename = db.Name+ ".bak";
                        string backupDestination = Path.Combine(path, filename);
                        Backup BackupObject = new()
                        {
                            Action = BackupActionType.Database,
                            Database = db.Name
                        };

                        BackupDeviceItem destination = new BackupDeviceItem(backupDestination, DeviceType.File);

                        BackupObject.Devices.Add(destination);
                        BackupObject.SqlBackup(_server);

                        response.Add("file", filename);
                    }
                }

                response.Add("status", "success");
                response.Add("message", "success");
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