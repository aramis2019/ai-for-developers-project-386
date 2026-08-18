using Meetly.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meetly.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext для Meetly. Работает с PostgreSQL через Npgsql.
/// Атомарность сквозной занятости (ADR 0001) обеспечивается на уровне БД:
/// generated tstzrange-колонка "during" + exclusion constraint (см. миграцию).
/// </summary>
public sealed class MeetlyDbContext(DbContextOptions<MeetlyDbContext> options) : DbContext(options)
{
    internal DbSet<EventTypeEntity> EventTypes => Set<EventTypeEntity>();
    internal DbSet<BookingEntity> Bookings => Set<BookingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventTypeEntity>(builder =>
        {
            builder.ToTable("event_types");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(64);
            builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(120);
            builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
            builder.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        });

        modelBuilder.Entity<BookingEntity>(builder =>
        {
            builder.ToTable("bookings");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).HasColumnName("id");
            builder.Property(b => b.EventTypeId).HasColumnName("event_type_id").HasMaxLength(64);
            builder.Property(b => b.EventTypeTitle).HasColumnName("event_type_title");
            builder.Property(b => b.Start).HasColumnName("start_at").HasColumnType("timestamptz");
            builder.Property(b => b.End).HasColumnName("end_at").HasColumnType("timestamptz");
            builder.Property(b => b.DurationMinutes).HasColumnName("duration_minutes");
            builder.Property(b => b.GuestName).HasColumnName("guest_name").HasMaxLength(120);
            builder.Property(b => b.GuestEmail).HasColumnName("guest_email").HasMaxLength(254);
            builder.Property(b => b.GuestNote).HasColumnName("guest_note").HasMaxLength(1000);
            builder.Property(b => b.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

            builder.HasIndex(b => b.Start);
            builder.HasIndex(b => b.EventTypeId);
        });
    }
}
