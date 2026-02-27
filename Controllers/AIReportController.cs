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
        private readonly IAIService _aiService;
        private readonly GroqAIService _groqService;
        private readonly RestaurantDBContext _dbContext;
        private readonly ISqlQueryService _sqlService;
        private readonly IConfiguration _configuration;

        public AIReportController(IAIService aiService, GroqAIService groqService, RestaurantDBContext dbContext, ISqlQueryService sqlService,
            ILanguageService languageService, ILocalizationService localizationService, IConfiguration configuration)
            : base(languageService, localizationService)
        {
            _aiService = aiService;
            _groqService = groqService;
            _dbContext = dbContext;
            _sqlService = sqlService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.isModelLoaded = !string.IsNullOrEmpty(_configuration["GeminiApiKey"]) || !string.IsNullOrEmpty(_configuration["GroqApiKey"]);
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
                string sqlPrompt = $"You are a Microsoft SQL Server (T-SQL) Expert. Based on the following database schema, generate a single T-SQL SELECT query to answer the user's question.\n\n" +
                                   $"CRITICAL RULES:\n" +
                                   $"- Use T-SQL syntax (Microsoft SQL Server).\n" +
                                   $"- Use 'TOP' for limiting rows (e.g., SELECT TOP 10 ...).\n" +
                                   $"- NEVER use 'LIMIT' (it will cause syntax errors).\n" +
                                   $"- Output ONLY the raw SQL query inside a markdown code block (```sql ... ```).\n\n" +
                                   $"[SCHEMA]\n{schema}\n\n" +
                                   $"[USER QUESTION]\n{request.Message}";

                string aiSqlResponse = await _aiService.GetResponseAsync(sqlPrompt);

                // Fallback to Groq if Gemini hits rate limits
                if (aiSqlResponse.Contains("TooManyRequests") || aiSqlResponse.Contains("429"))
                {
                    aiSqlResponse = await _groqService.GetResponseAsync(sqlPrompt);
                }

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
                        queryResult = $"Database Error: {ex.Message}";
                    }
                }
                else
                {
                    queryResult = "No specific data could be retrieved for this question.";
                }

                // 3. Step 2: Final Summary
                string finalPrompt = $"You are a sophisticated Restaurant Business Intelligence Analyst. Below is a user's question and the data retrieved from the database to answer it. Summarize the findings for the user professionally. If the data is an error or empty, explain why.\n\n[USER QUESTION]\n{request.Message}\n\n[SQL EXECUTED]\n{sql}\n\n[QUERY RESULT DATA]\n{queryResult}";

                string finalResponse = await _aiService.GetResponseAsync(finalPrompt);

                // Fallback to Groq if Gemini hits rate limits
                if (finalResponse.Contains("TooManyRequests") || finalResponse.Contains("429"))
                {
                    finalResponse = await _groqService.GetResponseAsync(finalPrompt);
                }

                return Json(new { response = finalResponse, sql = sql });
            }
            catch (Exception ex)
            {
                return Json(new { response = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        private string ExtractSql(string aiResponse)
        {
            if (string.IsNullOrEmpty(aiResponse)) return string.Empty;

            // Priority 1: Markdown Block
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

            // Priority 2: Direct SQL starting with SELECT
            string trimmed = aiResponse.Trim();
            if (trimmed.ToUpper().StartsWith("SELECT"))
            {
                // Try to find if there's text after the SQL and cut it
                int lineEnd = trimmed.IndexOf("\n\n");
                if (lineEnd > 0) return trimmed.Substring(0, lineEnd).Trim();
                return trimmed;
            }

            // Priority 3: Search for SELECT within text (more risky)
            if (aiResponse.ToUpper().Contains("SELECT "))
            {
                int start = aiResponse.ToUpper().IndexOf("SELECT ");
                string potentialSql = aiResponse.Substring(start);
                // Cut off at first double newline or markdown closing if any
                int end = potentialSql.IndexOf("```");
                if (end > 0) return potentialSql.Substring(0, end).Trim();

                return potentialSql.Trim();
            }

            return string.Empty;
        }
        public class ChatRequest
        {
            public string Message { get; set; }
        }
    }
}
