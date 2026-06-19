using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace Handog.web.Register
{
    public static class IdService
    {
        private static readonly string _connectionString = "Server=10.0.2.2,1433;Database=HANDOG_MOBILE;User Id=sa;Password=password123;TrustServerCertificate=True;";

        /// Generates an ID based on a prefix and the next auto-incrementing identity value.
        public static async Task<string> GenerateNextIdAsync(string prefix, string tableName, string identityColumnName)
        {
            int nextNumber = 1; // Default to 1 if table is empty

            // Query to find what the NEXT identity value will be in SQL Server
            string query = $@"SELECT IDENT_CURRENT('{tableName}') + 1 AS NextIdent";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        if (result != DBNull.Value && result != null)
                        {
                            nextNumber = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch
            {
                // Fallback approach if IDENT_CURRENT fails or lacks permissions:
                // Just fetch the maximum row count manually as a safety backup
                nextNumber = await GetMaxCountFallbackAsync(tableName, identityColumnName) + 1;
            }

            // Pads the number with 5 digits: e.g., 1 becomes "00001"
            return $"{prefix}{nextNumber:D5}";
        }

        private static async Task<int> GetMaxCountFallbackAsync(string tableName, string columnName)
        {
            string query = $"SELECT ISNULL(MAX({columnName}), 0) FROM {tableName}";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }
    }
}