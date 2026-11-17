using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Data;

public static class DatabaseBootstrap
{
    public static async Task EnsureReadyAsync(
        EmployeeDbContext context,
        string scriptPath,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string 'EmployeeManagement' was not found.");
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new InvalidOperationException("The connection string must name a database.");
        }

        if (!Regex.IsMatch(builder.InitialCatalog, "^[A-Za-z0-9_]+$"))
        {
            throw new InvalidOperationException("The database name contains unsupported characters.");
        }

        await EnsureDatabaseExistsAsync(builder, logger, cancellationToken);
        if (!await TableExistsAsync(context, cancellationToken))
        {
            logger.LogInformation("Creating the starter schema.");
            await ExecuteScriptAsync(context, scriptPath, cancellationToken);
        }

        await context.Database.MigrateAsync(cancellationToken);
    }

    private static async Task EnsureDatabaseExistsAsync(
        SqlConnectionStringBuilder builder,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var databaseName = builder.InitialCatalog;
        var master = new SqlConnectionStringBuilder(builder.ConnectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_ID(@name)";
        command.Parameters.Add(new SqlParameter("@name", databaseName));
        var id = await command.ExecuteScalarAsync(cancellationToken);
        if (id is not null and not DBNull)
        {
            return;
        }

        command.Parameters.Clear();
        command.CommandText = $"CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Created database {DatabaseName}.", databaseName);
    }

    private static async Task<bool> TableExistsAsync(EmployeeDbContext context, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CASE WHEN OBJECT_ID(N'dbo.Employee', N'U') IS NULL THEN 0 ELSE 1 END";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) == 1;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task ExecuteScriptAsync(
        EmployeeDbContext context,
        string scriptPath,
        CancellationToken cancellationToken)
    {
        var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            foreach (var batch in batches)
            {
                var sql = batch.Trim();
                if (sql.Length == 0)
                {
                    continue;
                }

                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
