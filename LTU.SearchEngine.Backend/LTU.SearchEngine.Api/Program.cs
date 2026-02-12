using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Infrastructure.Configuration;

namespace LTU.SearchEngine.Backend.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

        builder.Services.AddSingleton<ICrawlerSettingsLoader, JsonCrawlerSettingsLoader>();

        builder.Services.AddSingleton(serviceProvider =>
        {
            var crawlerSettingsLoader = serviceProvider.GetRequiredService<ICrawlerSettingsLoader>();
            return crawlerSettingsLoader.Load();
        });

        
        builder.Services.AddSingleton<SemaphoreProvider>();
     
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
