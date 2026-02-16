using LTU.SearchEngine.Backend.Core.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Configurations
{
    public class PageLinkConfiguration : IEntityTypeConfiguration<PageLink>
    {
        public void Configure(EntityTypeBuilder<PageLink> builder)
        {
            builder.HasKey(pl => new { pl.FromPageId, pl.ToPageId });

            builder.HasOne(pl => pl.FromPage)
                .WithMany(p => p.OutgoingLinks)
                .HasForeignKey(pl => pl.FromPageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pl => pl.ToPage)
                .WithMany()
                .HasForeignKey(pl => pl.ToPageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
