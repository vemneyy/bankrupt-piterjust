using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bankrupt_piterjust.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        
        public DatabaseService()
        {
            // Connection string for PostgreSQL
            // Credentials from the task description: postgres:postgres@10.155.1.210/piterjust
            _connectionString = "Host=10.155.1.210;Username=postgres;Password=postgres;Database=piterjust";
        }

        /// <summary>
        /// Execute a SQL command that does not return data
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(sql, connection);
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
            
            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Execute a SQL query that returns a single value
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string sql, Dictionary<string, object> parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(sql, connection);
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
            
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? default : (T)result;
        }

        /// <summary>
        /// Execute a SQL query that returns a data reader
        /// </summary>
        public async Task<DataTable> ExecuteReaderAsync(string sql, Dictionary<string, object> parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(sql, connection);
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
            
            using var reader = await command.ExecuteReaderAsync();
            var dataTable = new DataTable();
            dataTable.Load(reader);
            
            return dataTable;
        }
    }
}