using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace LTU.SearchEngine.Infrastructure.Data
{
    public class SearchDbContext : DbContext
    {
        public SearchDbContext(DbContextOptions<SearchDbContext> options)
    : base(options)
        {
        }
        public DbSet<Page> Pages { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<PageLink> PageLinks { get; set; }
        public DbSet<PageWordFrequency> PageWordFrequencies { get; set; }

        // --- Configuration (Fluent API) ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SearchDbContext).Assembly);
        }
    }
}
    
