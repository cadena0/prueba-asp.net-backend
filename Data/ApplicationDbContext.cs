using Microsoft.EntityFrameworkCore;
using RentasApi.Models;

namespace RentasApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<PropertyImage> PropertyImages { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Favorite> Favorites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de la relación Propiedad -> Dueño (Owner)
        modelBuilder.Entity<Property>()
            .HasOne(p => p.Owner)
            .WithMany(u => u.Properties)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de la relación Reserva -> Propiedad
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Property)
            .WithMany(p => p.Reservations)
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de la relación Reserva -> Usuario (Inquilino)
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Forzar nombres de tablas en minúsculas para PostgreSQL
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Property>().ToTable("properties");
        modelBuilder.Entity<PropertyImage>().ToTable("property_images");
        modelBuilder.Entity<Reservation>().ToTable("reservations");
        modelBuilder.Entity<Favorite>().ToTable("favorites");
    }
}