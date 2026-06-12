using Fundo.Applications.WebApi.Data;
using Fundo.Applications.WebApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fundo.Applications.WebApi
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private const string FrontendCorsPolicy = "Frontend";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<LoanManagementDbContext>(options =>
                options.UseSqlServer(_configuration.GetConnectionString("LoanManagementDb")));

            var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200" };

            services.AddCors(options =>
                options.AddPolicy(FrontendCorsPolicy, policy =>
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()));

            services.AddScoped<ILoanService, LoanService>();

            services.AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

            services.AddProblemDetails();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Unhandled exceptions are logged and returned as a generic
            // RFC 9110 problem response, never as a stack trace.
            app.UseExceptionHandler();

            app.UseSerilogRequestLogging();

            app.UseRouting();
            app.UseCors(FrontendCorsPolicy);
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
