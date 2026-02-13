using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Configuration;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using LTU.SearchEngine.Infrastructure.Data;

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
        
        builder.Services.AddOpenApi();

        // Registrera DbContextFactory (för trådsäkerhet med TPL Crawler)
        builder.Services.AddDbContextFactory<SearchDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SearchEngineDb;Trusted_Connection=True;MultipleActiveResultSets=true"));


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
