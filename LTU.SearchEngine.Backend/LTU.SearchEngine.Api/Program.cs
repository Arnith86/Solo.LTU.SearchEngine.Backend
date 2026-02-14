using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Configuration;
using LTU.SearchEngine.Infrastructure.Data;
using LTU.SearchEngine.Infrastructure.Indexing.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LTU.SearchEngine.Backend.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
      
        builder.Services.AddSingleton<ICrawlerSettingsLoader, JsonCrawlerSettingsLoader>();

        builder.Services.AddSingleton(serviceProvider =>
        {
            var crawlerSettingsLoader = serviceProvider.GetRequiredService<ICrawlerSettingsLoader>();
            return crawlerSettingsLoader.Load();
        });
        
        builder.Services.AddOpenApi();

        builder.Services.AddDbContextFactory<SearchDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SearchEngineDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

        builder.Services.AddTransient<IIndexRepository, SqlIndexRepository>();

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
