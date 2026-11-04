using System;
using System.Collections.Generic;
using Actian.EFCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore;

namespace DateDemo.Scaffolded;
public partial class ScaffoldedContext : DbContext
{
    public ScaffoldedContext(DbContextOptions<ScaffoldedContext> options) : base(options)
    {
    }

    public virtual DbSet<DateSample> DateSamples { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ingres").HasAnnotation("Actian:DbDelimitedCase", ActianCasing.Lower).HasAnnotation("Actian:DbNameCase", ActianCasing.Lower);
        modelBuilder.Entity<DateSample>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("$date__u000000fc00000000");
            entity.ToTable("date_samples").HasAnnotation("Actian:Duplicates", true).HasAnnotation("Actian:Journaling", false).HasAnnotation("Actian:Location", new[] { "ii_database" });
            entity.Property(e => e.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(e => e.Label).HasMaxLength(80).IsUnicode(false).HasColumnName("label");
            entity.Property(e => e.LeadTime).HasColumnName("lead_time");
            entity.Property(e => e.NativeValue).HasColumnName("native_value");
            entity.Property(e => e.NullableValue).HasColumnName("nullable_value");
            entity.Property(e => e.RequiredDate).HasDefaultValueSql("''").HasColumnName("required_date");
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
