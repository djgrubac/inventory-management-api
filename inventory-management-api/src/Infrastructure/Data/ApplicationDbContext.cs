using System.Reflection;
using Inventory_Management.Application.Common.Interfaces;
using RoleNames = Inventory_Management.Domain.Constants.Roles;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    // Named App* because IdentityDbContext already has Users and Roles (the Aspnet_* tables).
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<AppRole> AppRoles { get; set; }
    public DbSet<UserWarehousePermission> UserWarehousePermissions { get; set; }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Configure all string properties to use citext (case-insensitive) by default
        configurationBuilder.Properties<string>().HaveColumnType("citext");
        
        base.ConfigureConventions(configurationBuilder);
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Configure schema and case-sensitivity for PostgreSQL
        // builder.HasDefaultSchema("public");
        // builder.HasAnnotation("Relational:HistoryTableName", "__Efmigrationshistory");
        // builder.HasAnnotation("Relational:HistoryTableSchema", "public");
        
        // Configure Identity tables to use lowercase names
        builder.Entity<ApplicationUser>().ToTable("Aspnet_Users");
        builder.Entity<IdentityRole>().ToTable("Aspnet_Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("Aspnet_User_Roles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("Aspnet_User_Claims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("Aspnet_User_Logins");
        builder.Entity<IdentityUserToken<string>>().ToTable("Aspnet_User_Tokens");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("Aspnet_Role_Claims");

        base.OnModelCreating(builder);
        
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Order lines are history: a product that has been ordered must not be deleted.
        builder.Entity<OrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Stock rows are current state, not history, so empty ones may cascade with the product.
        // ProductDeleteCommandHandler refuses the delete while any warehouse still holds stock.
        builder.Entity<Stock>()
            .HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(pc => pc.Id);
            entity.Property(pc => pc.Caption).IsRequired();
        });

        builder.Entity<AppRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasIndex(r => r.Name).IsUnique();
            // Fixed ids and Uids: HasData needs the same values on every model build.
            entity.HasData(
                new AppRole { Id = 1, Uid = new Guid("6f1c0b6e-3a51-4b8e-9a63-2a7d8f0e1a01"), Name = RoleNames.Administrator },
                new AppRole { Id = 2, Uid = new Guid("6f1c0b6e-3a51-4b8e-9a63-2a7d8f0e1a02"), Name = RoleNames.Manager },
                new AppRole { Id = 3, Uid = new Guid("6f1c0b6e-3a51-4b8e-9a63-2a7d8f0e1a03"), Name = RoleNames.Staff });
        });

        builder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(u => u.Auth0UserId).IsUnique();
            entity.HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserWarehousePermission>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.WarehouseId }).IsUnique();
            entity.Property(p => p.Permission).HasConversion<string>();
            entity.HasOne(p => p.User)
                .WithMany(u => u.WarehousePermissions)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Permissions are current state, so they go away with the warehouse.
            entity.HasOne(p => p.Warehouse)
                .WithMany()
                .HasForeignKey(p => p.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
