using DiplomApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DiplomApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Reservation entity
            builder.Entity<Reservation>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
                entity.Property(r => r.Email).HasMaxLength(200);
                entity.Property(r => r.PhoneNumber).HasMaxLength(20);
                entity.Property(r => r.ReservationDateTime).IsRequired();
                entity.Property(r => r.MedicalSpecialty).IsRequired();
                entity.Property(r => r.UserId).IsRequired();
                
                // Create index on ReservationDateTime for faster queries
                entity.HasIndex(r => r.ReservationDateTime);
                
                // Unique constraint to prevent double booking (Race Condition protection)
                // One reservation per time slot and specialty combination
                entity.HasIndex(r => new { r.ReservationDateTime, r.MedicalSpecialty })
                    .IsUnique();
            });

            // Configure DoctorAvailability entity
            builder.Entity<DoctorAvailability>(entity =>
            {
                entity.HasKey(da => da.Id);
                entity.Property(da => da.DoctorId).IsRequired();
                entity.Property(da => da.Date).IsRequired();
                entity.Property(da => da.AvailableSlots).IsRequired();
                
                // Create indexes for faster queries
                entity.HasIndex(da => new { da.DoctorId, da.Date });
                entity.HasIndex(da => da.Date);
            });
        }
    }
}


