using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Api.Features;
using Portfolio.Api.Services;
using Portfolio.App;
using Portfolio.App.HistoricalPrice;
using Portfolio.App.HistoricalPrice.CoinGecko;
using Portfolio.App.Services;
using Portfolio.Domain.Interfaces;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.HistoricalPrice;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;


namespace Portfolio.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext() // Allows you to add properties to the log context dynamically
                                         //.Enrich.WithCallerInfo() // Automatically includes method and class names
                                         // add console as logging target
                .WriteTo.Console()
                // add a logging target for warnings and higher severity  logs
                // structured in JSON format
                .WriteTo.File(new JsonFormatter(), "important.json", restrictedToMinimumLevel: LogEventLevel.Warning)
                // add a rolling file for all logs
                .WriteTo.File("all-.logs", rollingInterval: RollingInterval.Day)
                // set default minimum level
                .MinimumLevel.Debug()
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder => builder
                        .WithOrigins("http://localhost:3000", "https://localhost:3000") // Replace with your React app's URL
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()); // Add this if you're using cookies or authentication
            });

            builder.Services.AddScoped<IUser, CurrentUser>();
            builder.Services.AddHttpContextAccessor();

            // Add services to the container.
            //builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHttpClient("CoinGeckoClient", client =>
            {
                client.BaseAddress = new Uri("https://api.coingecko.com");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30); // Set the timeout
            });
            builder.Services.AddSingleton(new MemoryCache(new MemoryCacheOptions()));
            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<ICryptoTransactionService, CryptoTransactionService>();
            builder.Services.AddScoped<IHoldingService, HoldingService>();
            builder.Services.AddScoped<IPriceHistoryApi, CoinGeckoPriceHistoryApi>();//(p => new PriceHistoryApiWithRetry(new YahooFinancePriceHistoryApi(), 3));
            builder.Services.AddScoped<IPriceHistoryService, PriceHistoryService>();
            builder.Services.AddScoped<IPriceHistoryStorageService, DbContextPriceHistoryStorageService>();


            var app = builder.Build();

            // Use CORS
            app.UseCors("AllowSpecificOrigin");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            //app.UseAuthorization();

            app.MapWalletEndpoints();
            app.MapPortfolioEndpoints();
            app.MapHoldingEndpoints();
            app.MapTransactionEndpoints();
            app.MapAuthenticationEndpoints();


            app.Run();
        }
    }
}
