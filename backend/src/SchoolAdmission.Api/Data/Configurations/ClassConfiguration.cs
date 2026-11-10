using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Api.Models.Entities;

namespace SchoolAdmission.Api.Data.Configurations;

public class ClassConfiguration : IEntityTypeConfiguration<ClassEntity>
{
    public void Configure(EntityTypeBuilder<ClassEntity> builder)
    {
        builder.ToTable("classes", t =>
        {
            t.HasCheckConstraint("ck_classes_total_seats_positive", "\"total_seats\" > 0");
            t.HasCheckConstraint("ck_classes_name_not_empty", "length(trim(\"name\")) > 0");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasMaxLength(64);

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("ix_classes_created_at");

        // 1-to-many relationship: One Class has many SeatHolds
        builder.HasMany(c => c.Holds)
            .WithOne(h => h.Class)
            .HasForeignKey(h => h.ClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
