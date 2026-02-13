using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace LTU.SearchEngine.Infrastructure.Data
{
    public class SearchDbContext : DbContext
    {
        public SearchDbContext(DbContextOptions<SearchDbContext> options)
    : base(options)
        {
        }
        // --- 1. Tabeller ---
        public DbSet<Page> Pages { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<PageLink> PageLinks { get; set; }
        public DbSet<PageWordFrequency> PageWordFrequencies { get; set; }

        // --- 2. Konfiguration (Fluent API) ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- PAGE ---
            // ER: url varchar UNIQUE
            modelBuilder.Entity<Page>()
                .HasIndex(p => p.Url)
                .IsUnique();

            // --- TERM ---
            // ER: normalized_word varchar UNIQUE
            modelBuilder.Entity<Term>()
                .HasIndex(t => t.Word)
                .IsUnique();

            // --- PAGE WORD FREQUENCY (Kopplingstabell) ---
            // ER: PK (page_id, term_id) -> Sammansatt nyckel
            modelBuilder.Entity<PageWordFrequency>()
                .HasKey(pwf => new { pwf.PageId, pwf.TermId });

            // Relationer
            modelBuilder.Entity<PageWordFrequency>()
                .HasOne(pwf => pwf.Page)
                .WithMany() // Vi har inte lagt till ICollection i Page än, så detta räcker
                .HasForeignKey(pwf => pwf.PageId);

            modelBuilder.Entity<PageWordFrequency>()
                .HasOne(pwf => pwf.Term)
                .WithMany()
                .HasForeignKey(pwf => pwf.TermId);

            // --- PAGE LINKS (Kopplingstabell) ---
            // ER: PK (from_page_id, to_page_id) -> Sammansatt nyckel
            modelBuilder.Entity<PageLink>()
                .HasKey(pl => new { pl.FromPageId, pl.ToPageId });

            // Relation: Från Sida A...
            modelBuilder.Entity<PageLink>()
                .HasOne(pl => pl.FromPage)
                .WithMany()
                .HasForeignKey(pl => pl.FromPageId)
                .OnDelete(DeleteBehavior.Restrict); // Förhindra cirkulära borttagningsregler

            // Relation: ...Till Sida B
            modelBuilder.Entity<PageLink>()
                .HasOne(pl => pl.ToPage)
                .WithMany()
                .HasForeignKey(pl => pl.ToPageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
    
