using DinkToPdf.Contracts;
using DinkToPdf;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Saffrat.Hubs;
using Saffrat.Models;
using Saffrat.Services;
using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddDbContext<RestaurantDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("default"));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "_auth";
        options.Cookie.HttpOnly = false;
        options.LoginPath = new PathString("/Account/Login");
        options.LogoutPath = new PathString("/Account/Logout");
        options.AccessDeniedPath = new PathString("/Account/Login");
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
    });

//Localization
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews().AddViewLocalization();
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1); // 1 hour expiry
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//Services
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddScoped<Saffrat.Services.AccountingEngine.IAccountingEngine, Saffrat.Services.AccountingEngine.DefaultAccountingEngine>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAIService, GeminiAIService>();
builder.Services.AddScoped<GroqAIService>();
builder.Services.AddScoped<ISqlQueryService, SqlQueryService>();
builder.Services.AddScoped<IDateTimeService, DateTimeService>();




builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.Configure<DataProtectionTokenProviderOptions>(opts => opts.TokenLifespan = TimeSpan.FromHours(10));
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
});

//store keys in file system
var keysDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "AppFiles/Keys");
if (!Directory.Exists(keysDirectoryPath))
{
    Directory.CreateDirectory(keysDirectoryPath);
}
builder.Services.AddDataProtection()
      .PersistKeysToFileSystem(new DirectoryInfo(keysDirectoryPath))
      .SetApplicationName("Saffrat");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var languageService = scope.ServiceProvider.GetRequiredService<ILanguageService>();
    var languages = languageService.GetLanguages();

    var options = new RequestLocalizationOptions();

    List<CultureInfo> uicultures = new();
    List<CultureInfo> cultures = new();

    foreach (var culture in languages.Select(x => x.Culture).ToArray())
    {
        var cul = new CultureInfo(culture);
        cul.DateTimeFormat.Calendar = new GregorianCalendar();
        uicultures.Add(cul);
    }

    foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures).ToList())
    {
        var cul = new CultureInfo(culture.Name);
        cul.DateTimeFormat.Calendar = new GregorianCalendar();
        cultures.Add(cul);
    }

    options.DefaultRequestCulture = new RequestCulture(languageService.GetDefaultRegion() ?? "en-US", languageService.GetDefaultLanguage() ?? "en");

    options.SupportedCultures = cultures;
    options.SupportedUICultures = uicultures;

    app.UseRequestLocalization(options);
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");

app.UseCors(options => options.AllowAnyOrigin());

app.Run();
