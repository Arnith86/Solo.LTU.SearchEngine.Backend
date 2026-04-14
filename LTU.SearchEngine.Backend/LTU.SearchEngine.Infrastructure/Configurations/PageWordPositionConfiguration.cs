using LTU.SearchEngine.Backend.Core.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LTU.SearchEngine.Infrastructure.Configurations;

public class PageWordPositionConfiguration : IEntityTypeConfiguration<PageWordPosition>
{
    public void Configure(EntityTypeBuilder<PageWordPosition> builder)
    {
        builder.HasKey(pwp => new { pwp.PageId, pwp.TermId, pwp.Position });

        builder.HasOne(pwp => pwp.Page)
            .WithMany(p => p.PagePositions)
            .HasForeignKey(pwp => pwp.PageId);

        builder.HasOne(pwp => pwp.Term)
            .WithMany(t => t.PagePositions)
            .HasForeignKey(pwp => pwp.TermId);
    }
}
