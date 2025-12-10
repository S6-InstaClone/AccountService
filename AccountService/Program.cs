using AccountService.Business;
using AccountService.Data;
using AccountService.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace AccountService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("AccountService"))
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                           .AddHttpClientInstrumentation()
                           .AddPrometheusExporter();
                });

            builder.Services.AddControllers();

            // Build connection string from environment variables
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "account-db";
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "accounts";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER")
                ?? throw new InvalidOperationException("DB_USER environment variable is required");
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD")
                ?? throw new InvalidOperationException("DB_PASSWORD environment variable is required");

            var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

            builder.Services.AddDbContext<AccountRepository>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddEndpointsApiExplorer();

            // Only enable Swagger in Development
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddSwaggerGen();
            }

            builder.Services.AddScoped<BlobService>();
            builder.Services.AddScoped<ProfileService>();
            builder.Services.AddHttpClient<KeycloakService>();

            // RabbitMQ configuration from environment variables
            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
            var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USER")
                ?? throw new InvalidOperationException("RABBITMQ_USER environment variable is required");
            var rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                ?? throw new InvalidOperationException("RABBITMQ_PASSWORD environment variable is required");

            builder.Services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitHost, "/", h =>
                    {
                        h.Username(rabbitUser);
                        h.Password(rabbitPassword);
                    });
                });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AccountRepository>();
                db.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseOpenTelemetryPrometheusScrapingEndpoint();

            // Only use HTTPS redirection in production with proper certificates
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}