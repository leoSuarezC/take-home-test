using Fundo.Applications.WebApi;
using Fundo.Applications.WebApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fundo.Services.Tests.Integration
{
    /// <summary>
    /// Boots the real application pipeline but replaces SQL Server with an
    /// in-memory SQLite database, so integration tests are fast, isolated
    /// and require no external infrastructure.
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Startup>
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<LoanManagementDbContext>));
                services.AddDbContext<LoanManagementDbContext>(options => options.UseSqlite(_connection));

                using var scope = services.BuildServiceProvider().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<LoanManagementDbContext>();
                dbContext.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection.Dispose();
        }
    }
}
