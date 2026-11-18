using EmployeeManagement.Data;
using EmployeeManagement.Presentation;
using EmployeeManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("EmployeeManagement");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("Put ConnectionStrings:EmployeeManagement in appsettings.json (or user secrets) first.");
            return;
        }

        builder.Services.AddDbContextFactory<EmployeeDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddSingleton<ProjectReportService>();
        builder.Services.AddSingleton<EmployeeReportService>();
        builder.Services.AddSingleton<PayrollReportService>();
        builder.Services.AddSingleton<EmployeeFilterService>();
        builder.Services.AddSingleton<RawSqlReportService>();
        builder.Services.AddSingleton<EmployeeCommandService>();
        builder.Services.AddSingleton<DatabaseFeatureService>();
        builder.Services.AddSingleton<ConsoleMenu>();

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        var factory = host.Services.GetRequiredService<IDbContextFactory<EmployeeDbContext>>();

        try
        {
            await using var context = await factory.CreateDbContextAsync();
            var scriptPath = ResolveScriptPath(builder.Environment.ContentRootPath);
            await DatabaseBootstrap.EnsureReadyAsync(context, scriptPath, logger);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database startup failed.");
            Console.WriteLine("Couldn't get the database ready.");
            Console.WriteLine(exception.Message);
            Console.WriteLine("This expects SQL Server Express LocalDB, instance (localdb)\\MSSQLLocalDB, database EmployeeManagementDb.");
            Console.WriteLine("Start that, then run the app again.");
            return;
        }

        await host.Services.GetRequiredService<ConsoleMenu>().RunAsync();
    }

    private static string ResolveScriptPath(string contentRoot)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Database", "01_CreateSchemaAndSeed.sql"),
            Path.Combine(contentRoot, "Database", "01_CreateSchemaAndSeed.sql"),
            Path.Combine(contentRoot, "..", "Database", "01_CreateSchemaAndSeed.sql")
        };

        var match = candidates.FirstOrDefault(File.Exists);
        if (match is null)
        {
            throw new FileNotFoundException("The database setup script was not found.", candidates[0]);
        }

        return match;
    }
}
