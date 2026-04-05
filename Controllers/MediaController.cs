using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using System.Security.Claims;
using Saffrat.Services;
using System.Data;
using Microsoft.AspNetCore.StaticFiles;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.IO;
using MimeKit;

namespace Saffrat.Controllers
{
    public class MediaController : BaseController
    {
		private readonly ILogger<MediaController> _logger;
		private readonly RestaurantDBContext _dbContext;

        public MediaController(ILogger<MediaController> logger, RestaurantDBContext dbContext,
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
        public async Task<IActionResult> EmployeeImage(string name)
        {
            try
            {
                var pattern = Path.GetInvalidFileNameChars();
                if(name.Any(pattern.Contains))
                    return BadRequest();

                string fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/Company/{0}/{1}/{2}", 1, "Employee", name)));

                FileInfo file = new(fullpath);
                if (!file.Exists)//check file exsit or not  
                {
                    fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/{0}/{1}", "Placeholder", "placeholder.png")));
                }

                var content = await System.IO.File.ReadAllBytesAsync(fullpath);
                new FileExtensionContentTypeProvider()
                    .TryGetContentType(fullpath, out string contentType);
                return File(content, contentType, name);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> EmployeeAttachment(string name)
        {
            try
            {
                var pattern = Path.GetInvalidFileNameChars();
                if (name.Any(pattern.Contains))
                    return BadRequest();

                string fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/Company/{0}/{1}/{2}", 1, "Employee", name)));

                FileInfo file = new(fullpath);
                if (!file.Exists)//check file exsit or not  
                {
                    return BadRequest();
                }

                var content = await System.IO.File.ReadAllBytesAsync(fullpath);
                new FileExtensionContentTypeProvider()
                    .TryGetContentType(fullpath, out string contentType);
                return File(content, contentType, name);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> TableImage(string name)
        {
            try
            {
                var pattern = Path.GetInvalidFileNameChars();
                if (name.Any(pattern.Contains))
                    return BadRequest();

                string fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/Company/{0}/{1}/{2}", 1, "Tables", name)));

                FileInfo file = new(fullpath);
                if (!file.Exists)//check file exsit or not  
                {
                    fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/{0}/{1}", "Placeholder", "table.png")));
                }

                var content = await System.IO.File.ReadAllBytesAsync(fullpath);
                new FileExtensionContentTypeProvider()
                    .TryGetContentType(fullpath, out string contentType);
                return File(content, contentType, name);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> FoodGroupImage(string name)
        {
            try
            {
                var pattern = Path.GetInvalidFileNameChars();
                if (name.Any(pattern.Contains))
                    return BadRequest();

                string fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/Company/{0}/{1}/{2}", 1, "FoodGroups", name)));

                FileInfo file = new(fullpath);
                if (!file.Exists)//check file exsit or not  
                {
                    fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/{0}/{1}", "Placeholder", "placeholder.png")));
                }

                var content = await System.IO.File.ReadAllBytesAsync(fullpath);
                new FileExtensionContentTypeProvider()
                    .TryGetContentType(fullpath, out string contentType);
                return File(content, contentType, name);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> ProductImage(string name)
        {
            try
            {
                var pattern = Path.GetInvalidFileNameChars();
                if (name.Any(pattern.Contains))
                    return BadRequest();

                string fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/Company/{0}/{1}/{2}", 1, "Products", name)));

                FileInfo file = new(fullpath);
                if (!file.Exists)//check file exsit or not  
                {
                    fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/{0}/{1}", "Placeholder", "placeholder.png")));
                }

                var content = await System.IO.File.ReadAllBytesAsync(fullpath);
                new FileExtensionContentTypeProvider()
                    .TryGetContentType(fullpath, out string contentType);
                return File(content, contentType, name);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult DatabaseBackup(string name)
        {
            try
            {
                var pattern = Path.GetInvalidFileNameChars();
                if (name.Any(pattern.Contains))
                    return BadRequest();

                string fullpath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/Company/{0}/{1}/{2}", 1, "Backup", name)));

                FileInfo file = new(fullpath);
                if (!file.Exists)//check file exsit or not  
                {
                    return BadRequest();
                }

                string mimeType = MimeTypes.GetMimeType(name);

                FileStream fileStream = new(fullpath, FileMode.Open, FileAccess.Read);
                return File(fileStream, mimeType, name);
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}