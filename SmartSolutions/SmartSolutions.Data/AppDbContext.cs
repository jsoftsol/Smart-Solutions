// SmartSolutions.Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ItemCategory>               ItemCategories              { get; set; }
    public DbSet<ItemName>                   ItemNames                   { get; set; }
    public DbSet<Vendor>                     Vendors                     { get; set; }
    public DbSet<Technician>                 Technicians                 { get; set; }
    public DbSet<ExpenseCategory>            ExpenseCategories           { get; set; }
    public DbSet<PaymentChannel>             PaymentChannels             { get; set; }
    public DbSet<BusinessInfo>               BusinessInfos               { get; set; }
    public DbSet<Customer>                   Customers                   { get; set; }
    public DbSet<PrintOrder>                 PrintOrders                 { get; set; }
    public DbSet<PrintOrderLine>             PrintOrderLines             { get; set; }
    public DbSet<PrintOrderVendorAssignment> PrintOrderVendorAssignments { get; set; }
    public DbSet<PrintOrderPayment>          PrintOrderPayments          { get; set; }
    public DbSet<HaierJob>                   HaierJobs                   { get; set; }
    public DbSet<HaierJobPayment>            HaierJobPayments            { get; set; }
    public DbSet<Expense>                    Expenses                    { get; set; }
    public DbSet<AppUser>                    AppUsers                    { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed BusinessInfo singleton row
        modelBuilder.Entity<BusinessInfo>().HasData(new BusinessInfo
        {
            Id = 1, Name = "Smart Solutions", Ntn = "7569020-2",
            Address = "", Phone1 = ""
        });

        // Seed default payment channels
        modelBuilder.Entity<PaymentChannel>().HasData(
            new PaymentChannel { Id = 1, Name = "Cash"      },
            new PaymentChannel { Id = 2, Name = "Easypaisa" },
            new PaymentChannel { Id = 3, Name = "Bank"      }
        );

        // Store enums as strings for readability in the database
        modelBuilder.Entity<PrintOrder>()
            .Property(o => o.Status)
            .HasConversion<string>();

        modelBuilder.Entity<PrintOrderLine>()
            .Property(l => l.RateType)
            .HasConversion<string>();

        modelBuilder.Entity<PrintOrderLine>()
            .Property(l => l.Unit)
            .HasConversion<string>();

        modelBuilder.Entity<HaierJob>()
            .Property(j => j.JobType)
            .HasConversion<string>();

        modelBuilder.Entity<HaierJob>()
            .Property(j => j.Status)
            .HasConversion<string>();

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // No cascade delete from audit-trail FK columns to AppUser
        foreach (var fk in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(AppUser)))
        {
            fk.DeleteBehavior = DeleteBehavior.NoAction;
        }

        // decimal(18,2) for all money columns
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
    }
}
