using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Saffrat.Models;

namespace Saffrat.Services
{
    public interface ISqlQueryService
    {
        Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql);
    }

    public class SqlQueryService : ISqlQueryService
    {
        private readonly RestaurantDBContext _dbContext;

        public SqlQueryService(RestaurantDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql)
        {
            // Security Check: Only allow SELECT queries
            string trimmedSql = sql.TrimStart().ToUpper();
            if (!trimmedSql.StartsWith("SELECT"))
            {
                throw new Exception("Only SELECT queries are allowed for security reasons.");
            }

            var results = new List<Dictionary<string, object>>();
            var connection = _dbContext.Database.GetDbConnection();

            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }

            return results;
        }
    }
}
