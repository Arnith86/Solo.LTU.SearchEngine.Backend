using LTU.SearchEngine.Backend.Core.Entities;
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
            .WithOne(p => p.HtmlMetaData)
            .HasForeignKey<HtmlMetaData>(hmd => hmd.PageId)
            .OnDelete(DeleteBehavior.Cascade);
   }
}
