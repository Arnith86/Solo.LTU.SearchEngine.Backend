using LTU.SearchEngine.Backend.Core.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Configurations
{
    public class PageWordFrequencyConfiguration : IEntityTypeConfiguration<PageWordFrequency>
    {
        public void Configure(EntityTypeBuilder<PageWordFrequency> builder)
        {
            builder.HasKey(pwf => new { pwf.PageId, pwf.TermId });

            builder.HasOne(pwf => pwf.Page)
                .WithMany(p => p.WordFrequencies)
                .HasForeignKey(pwf => pwf.PageId);

            builder.HasOne(pwf => pwf.Term)
                .WithMany(t => t.PageFrequencies)
                .HasForeignKey(pwf => pwf.TermId);
        }
    }
}
