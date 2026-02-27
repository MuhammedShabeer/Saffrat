using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Services;
using System.Text;

namespace Saffrat.Controllers
{
    [Authorize(Roles = "admin")]
    public class AIReportController : BaseController
    {
        private readonly IGeminiAIService _aiService;
        private readonly RestaurantDBContext _dbContext;
        private readonly ISqlQueryService _sqlService;
        private readonly IConfiguration _configuration;

        public AIReportController(IGeminiAIService aiService, RestaurantDBContext dbContext, ISqlQueryService sqlService,
            ILanguageService languageService, ILocalizationService localizationService, IConfiguration configuration)
            : base(languageService, localizationService)
        {
            _aiService = aiService;
            _dbContext = dbContext;
            _sqlService = sqlService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.isModelLoaded = !string.IsNullOrEmpty(_configuration["GeminiApiKey"]);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                // 1. Load Schema Context
                string schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "AppFiles", "AI", "SchemaGuide.txt");
                string schema = System.IO.File.Exists(schemaPath) ? await System.IO.File.ReadAllTextAsync(schemaPath) : "";

                // 2. Step 1: Generate SQL Query
                string sqlPrompt = $"You are a SQL Expert. Based on the following database schema, generate a single T-SQL SELECT query to answer the user's question. Output ONLY the raw SQL query inside a markdown code block (```sql ... ```).\n\n[SCHEMA]\n{schema}\n\n[USER QUESTION]\n{request.Message}";
                string aiSqlResponse = await _aiService.GetResponseAsync(sqlPrompt);

                string sql = ExtractSql(aiSqlResponse);
                string queryResult = "";

                if (!string.IsNullOrEmpty(sql))
                {
                    try
                    {
                        var data = await _sqlService.ExecuteQueryAsync(sql);
                        queryResult = System.Text.Json.JsonSerializer.Serialize(data);
                    }
                    catch (Exception ex)
                    {
                        queryResult = $"Error executing SQL: {ex.Message}";
                    }
                }

                // 3. Step 2: Final Summary
                string finalPrompt = $"You are a sophisticated Restaurant Business Intelligence Analyst. Below is a user's question and the data retrieved from the database to answer it. Summarize the findings for the user professionally. If the data is an error or empty, explain why.\n\n[USER QUESTION]\n{request.Message}\n\n[SQL EXECUTED]\n{sql}\n\n[QUERY RESULT DATA]\n{queryResult}";
                string finalResponse = await _aiService.GetResponseAsync(finalPrompt);

                if (finalResponse.Contains("TooManyRequests") || finalResponse.Contains("429"))
                {
                    return Json(new { response = "⚠️ **Rate Limit Reached**: You've made too many requests in a short time. Please wait about 15-30 seconds before asking your next question." });
                }

                return Json(new { response = finalResponse });
            }
            catch (Exception ex)
            {
                return Json(new { response = $"An error occurred while processing your request: {ex.Message}" });
            }
        }

        private string ExtractSql(string aiResponse)
        {
            if (aiResponse.Contains("TooManyRequests") || aiResponse.Contains("429"))
                return string.Empty;

            if (aiResponse.Contains("```sql"))
            {
                int start = aiResponse.IndexOf("```sql") + 6;
                int end = aiResponse.IndexOf("```", start);
                if (end > start)
                    return aiResponse.Substring(start, end - start).Trim();
            }
            else if (aiResponse.Contains("```"))
            {
                int start = aiResponse.IndexOf("```") + 3;
                int end = aiResponse.IndexOf("```", start);
                if (end > start)
                    return aiResponse.Substring(start, end - start).Trim();
            }

            // Fallback: Check if it starts with SELECT
            if (aiResponse.Trim().ToUpper().StartsWith("SELECT"))
                return aiResponse.Trim();

            return string.Empty;
        }
        public class ChatRequest
        {
            public string Message { get; set; }
        }
    }
}
