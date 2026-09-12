using cashly.src.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace cashly.src.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /* RELAZIONI */
        modelBuilder
            .Entity<Category>() //
            .HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<Transaction>() //
            .HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder
            .Entity<Transaction>() //
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<Subscription>()
            .HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<Subscription>()
            .HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder
            .Entity<Subscription>()
            .Property(s => s.Frequency)
            .HasConversion<string>()
            .HasMaxLength(50);

        // imposta la email come unique
        modelBuilder
            .Entity<User>() //
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.HasPostgresEnum<TransactionType>();
    }
}
