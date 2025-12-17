using System.Data;
using Microsoft.Data.SqlClient;

namespace ReleaseFlow.Data;

public class SqlHelper
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlHelper> _logger;

    public SqlHelper(SqlConnectionFactory connectionFactory, ILogger<SqlHelper> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE)
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing non-query: {Sql}", sql);
            throw;
        }
    }

    /// <summary>
    /// Executes a scalar query and returns a single value
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(string sql, params SqlParameter[] parameters)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            var result = await command.ExecuteScalarAsync();
            
            if (result == null || result == DBNull.Value)
                return default;

            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scalar: {Sql}", sql);
            throw;
        }
    }

    /// <summary>
    /// Executes a query and returns a list of objects
    /// </summary>
    public async Task<List<T>> ExecuteReaderAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        var results = new List<T>();

        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing reader: {Sql}", sql);
            throw;
        }
    }

    /// <summary>
    /// Executes a query and returns a single object or null
    /// </summary>
    public async Task<T?> ExecuteReaderSingleAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters) where T : class
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            using var command = new SqlCommand(sql, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return mapper(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing reader single: {Sql}", sql);
            throw;
        }
    }

    /// <summary>
    /// Executes a query within a transaction
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<SqlConnection, SqlTransaction, Task<T>> action)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var result = await action(connection, transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Helper to safely get value from SqlDataReader
    /// </summary>
    public static T? GetValue<T>(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        
        if (reader.IsDBNull(ordinal))
            return default;

        return (T)reader.GetValue(ordinal);
    }

    /// <summary>
    /// Helper to safely get string value
    /// </summary>
    public static string GetString(SqlDataReader reader, string columnName, string defaultValue = "")
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : reader.GetString(ordinal);
    }

    /// <summary>
    /// Helper to safely get int value
    /// </summary>
    public static int GetInt32(SqlDataReader reader, string columnName, int defaultValue = 0)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt32(ordinal);
    }

    /// <summary>
    /// Helper to safely get bool value
    /// </summary>
    public static bool GetBoolean(SqlDataReader reader, string columnName, bool defaultValue = false)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : reader.GetBoolean(ordinal);
    }

    /// <summary>
    /// Helper to safely get DateTime value
    /// </summary>
    public static DateTime? GetDateTime(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
