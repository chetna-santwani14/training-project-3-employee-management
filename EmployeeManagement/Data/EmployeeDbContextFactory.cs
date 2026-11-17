using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EmployeeManagement.Data;

public class EmployeeDbContextFactory : IDesignTimeDbContextFactory<EmployeeDbContext>
{
    public EmployeeDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("EmployeeManagement")
            ?? throw new InvalidOperationException("Connection string 'EmployeeManagement' was not found.");

        var options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new EmployeeDbContext(options);
    }
}
