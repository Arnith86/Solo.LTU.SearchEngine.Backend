using Microsoft.EntityFrameworkCore;

namespace LTU.SearchEngine.Infrastructure
{
    public class SearchDbContext : DbContext
    {
        public SearchDbContext(DbContextOptions<SearchDbContext> options)
    : base(options)
        {
        }
    }
}
