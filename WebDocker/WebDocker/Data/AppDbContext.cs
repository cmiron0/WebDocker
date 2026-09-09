using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components.DesignTokens;

namespace WebDocker.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
    {

       
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DockerServer> DockerServers { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Configuración estándar de Identity (tablas, claves, índices y relaciones).
            base.OnModelCreating(builder);

            // Renombrar las tablas de Identity (por defecto se llaman AspNetUsers, AspNetRoles, ...).
            builder.Entity<AppUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserPasskey<string>>().ToTable("UserPasskeys");


            // AuditLog
            builder.Entity<AuditLog>(b =>
            {
                b.ToTable("AuditLogs");
                b.HasKey(e => e.Id);
                b.Property(e => e.Id).ValueGeneratedOnAdd(); 
                b.Property(e => e.UserName);
                b.Property(e => e.Action).IsRequired().HasDefaultValue("");
                b.Property(e => e.Target);
                b.Property(e => e.Success).IsRequired().HasDefaultValue(false);
                b.Property(e => e.Details);
                b.Property(e => e.Timestamp).IsRequired();

                b.HasIndex(e => e.Timestamp);
                b.HasIndex(e => new { e.UserName, e.Timestamp });
            });

            // DockerServers
            builder.Entity<DockerServer>(b =>
            {
                b.ToTable("DockerServers");
                b.HasKey(e => e.Id);
                b.Property(e => e.Name).IsRequired().HasMaxLength(100);
                b.Property(e => e.Host).IsRequired().HasMaxLength(255);
                b.Property(e => e.Port).HasDefaultValue(2375);
                b.Property(e => e.OperatingSystem).IsRequired().HasMaxLength(20).HasDefaultValue("Linux");
                b.Property(e => e.UseTls).HasDefaultValue(false);
                b.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                b.Property(e => e.UserId).IsRequired();

                b.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(e => e.UserId);
            });

            // Permissions (RBAC)
            builder.Entity<Permission>(b =>
            {
                b.ToTable("Permissions");
                b.HasKey(e => e.Id);
                b.Property(e => e.Code).IsRequired().HasMaxLength(100);
                b.Property(e => e.Description).IsRequired().HasMaxLength(255);
                b.HasIndex(e => e.Code).IsUnique();
            });

            // RefreshTokens
            builder.Entity<RefreshToken>(b =>
            {
                b.ToTable("RefreshTokens");
                b.HasKey(e => e.Id);
                b.Property(e => e.Id).ValueGeneratedOnAdd();
                b.Property(e => e.Token).IsRequired();
                b.Property(e => e.UserId).IsRequired();
                b.Property(e => e.ExpiresAt);
                b.Property(e => e.Revoked);
                b.HasIndex(e => e.Token).IsUnique();
                b.HasIndex(e => e.UserId);

                b.HasOne<AppUser>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // RolePersmissions
            builder.Entity<RolePermission>(b =>
            {
                b.ToTable("RolePermissions");
                b.HasKey(e => new { e.RoleId, e.PermissionId });
                b.HasOne(e => e.Permission)
                    .WithMany()
                    .HasForeignKey(e => e.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(e => e.RoleId);
            });


        }

    }
}
