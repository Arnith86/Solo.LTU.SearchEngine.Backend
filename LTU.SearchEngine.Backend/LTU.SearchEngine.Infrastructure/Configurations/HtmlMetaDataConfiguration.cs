using LTU.SearchEngine.Backend.Core.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LTU.SearchEngine.Infrastructure.Configurations;

public class HtmlMetaDataConfiguration : IEntityTypeConfiguration<HtmlMetaData>
{
    public void Configure(EntityTypeBuilder<HtmlMetaData> builder)
    {
        builder.HasKey(hmd => hmd.PageId);

        builder.HasOne(hmd => hmd.Page)
            .WithMany(p => p.HtmlMetaEntries)
            .HasForeignKey(hmd => hmd.PageId);
   }
}
