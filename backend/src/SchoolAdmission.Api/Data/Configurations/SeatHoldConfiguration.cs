using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Api.Models.Entities;

namespace SchoolAdmission.Api.Data.Configurations;

public class SeatHoldConfiguration : IEntityTypeConfiguration<SeatHoldEntity>
{
    public void Configure(EntityTypeBuilder<SeatHoldEntity> builder)
    {
        builder.ToTable("seat_holds", t =>
        {
            t.HasCheckConstraint("ck_seat_holds_status", "\"status\" IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
            t.HasCheckConstraint("ck_seat_holds_student_id_format", "\"student_id\" ~ '^[0-9]{17}$'");
        });

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .HasColumnName("id")
            .HasMaxLength(64);

        builder.Property(h => h.ClassId)
            .HasColumnName("class_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(h => h.StudentId)
            .HasColumnName("student_id")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(h => h.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(h => h.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(h => h.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Foreign Key
        builder.HasOne(h => h.Class)
            .WithMany(c => c.Holds)
            .HasForeignKey(h => h.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance Indexes
        builder.HasIndex(h => h.ClassId)
            .HasDatabaseName("ix_seat_holds_class_id");

        builder.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt })
            .HasDatabaseName("ix_seat_holds_class_status_expires");

        builder.HasIndex(h => new { h.StudentId, h.ClassId })
            .HasDatabaseName("ix_seat_holds_student_class");

        builder.HasIndex(h => h.ExpiresAt)
            .HasDatabaseName("ix_seat_holds_sweeper")
            .HasFilter("\"status\" = 'HELD'");
    }
}
