using LTU.SearchEngine.Backend.Core.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LTU.SearchEngine.Infrastructure.Configurations;

public class PdfMetaDataConfiguration : IEntityTypeConfiguration<PdfMetaData>
{
    public void Configure(EntityTypeBuilder<PdfMetaData> builder)
    {
        builder.HasKey(pmd => pmd.PageId);

        builder.HasOne(pmd =>pmd.Page)
            .WithOne(p => p.PdfMetaData)
            .HasForeignKey<PdfMetaData>(hmd => hmd.PageId)
            .OnDelete(DeleteBehavior.Cascade);
   }
}
