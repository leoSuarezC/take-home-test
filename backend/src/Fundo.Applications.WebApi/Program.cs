using Fundo.Applications.WebApi.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Fundo.Applications.WebApi
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var host = CreateWebHostBuilder(args).Build();

                ApplyMigrations(host);

                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled WebApi exception: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Application shutting down.");
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
        }

        private static void ApplyMigrations(IWebHost host)
        {
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LoanManagementDbContext>();
            dbContext.Database.Migrate();
        }
    }
}
