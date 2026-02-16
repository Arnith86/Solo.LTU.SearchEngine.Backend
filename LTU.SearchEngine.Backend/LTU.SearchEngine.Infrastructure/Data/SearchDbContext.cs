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

            modelBuilder.Entity<Page>()
                .HasIndex(p => p.Url)
                .IsUnique();

            modelBuilder.Entity<Term>()
                .HasIndex(t => t.Word)
                .IsUnique();

            modelBuilder.Entity<PageWordFrequency>()
                .HasKey(pwf => new { pwf.PageId, pwf.TermId });

            modelBuilder.Entity<PageWordFrequency>()
                .HasOne(pwf => pwf.Page)
                .WithMany(p => p.WordFrequencies) 
                .HasForeignKey(pwf => pwf.PageId);

            modelBuilder.Entity<PageWordFrequency>()
                .HasOne(pwf => pwf.Term)
                .WithMany(t => t.PageFrequencies) 
                .HasForeignKey(pwf => pwf.TermId);

            modelBuilder.Entity<PageLink>()
                .HasKey(pl => new { pl.FromPageId, pl.ToPageId });


            modelBuilder.Entity<PageLink>()
                .HasOne(pl => pl.FromPage)
                .WithMany(p => p.OutgoingLinks) 
                .HasForeignKey(pl => pl.FromPageId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<PageLink>()
                .HasOne(pl => pl.ToPage)
                .WithMany()
                .HasForeignKey(pl => pl.ToPageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
    
