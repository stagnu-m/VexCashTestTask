using Microsoft.EntityFrameworkCore;
using VexCash.SmsService.Models;

namespace VexCash.SmsService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<MessageTemplate> Templates { get; set; } = null!;
        public DbSet<SmsLog> SmsLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(32);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
                entity.Property(c => c.CreditNumber).IsRequired().HasMaxLength(50);
                entity.Property(c => c.Amount).IsRequired();
            });

            modelBuilder.Entity<MessageTemplate>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Content).IsRequired().HasMaxLength(1000);
                entity.Property(t => t.Type).HasConversion<string>();
            });

            modelBuilder.Entity<SmsLog>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.TemplateType).HasConversion<string>();
            });
        }
    }
}
