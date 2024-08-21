using Portfolio.Api.Features;
using Portfolio.Api.Services;
using Portfolio.App;
using Portfolio.App.Services;
using Portfolio.Domain;
using Portfolio.Infrastructure;


namespace Portfolio.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder => builder
                        .WithOrigins("http://localhost:3000") // Replace with your React app's URL
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()); // Add this if you're using cookies or authentication
            });

            builder.Services.AddScoped<IUser, CurrentUser>();
            builder.Services.AddHttpContextAccessor();

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<ICryptoTransactionService, CryptoTransactionService>();

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

            app.UseAuthorization();

            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();

            app.MapWalletEndpoints();
            app.MapPortfolioEndpoints();
            app.MapTransactionEndpoints();


            app.Run();
        }
    }
}
