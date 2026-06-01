using cashly.src.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace cashly.src.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

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
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder
            .Entity<Transaction>() //
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .OnDelete(DeleteBehavior.Cascade);

        // imposta la email come unique
        modelBuilder
            .Entity<User>() //
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.HasPostgresEnum<TransactionType>();
    }
}
