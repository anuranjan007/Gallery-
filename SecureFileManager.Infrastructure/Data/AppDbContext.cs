using Microsoft.EntityFrameworkCore;
using SecureFileManager.Domain.Entities;

namespace SecureFileManager.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
        public DbSet<Folder> Folders => Set<Folder>();
        public DbSet<FileRecord> Files => Set<FileRecord>();
        public DbSet<EncryptionKey> EncryptionKeys => Set<EncryptionKey>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<AdminOtp> AdminOtps => Set<AdminOtp>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AdminUser>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.Username).IsUnique();
            });

            modelBuilder.Entity<Folder>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => new { x.ParentFolderId, x.Name }).IsUnique();
                b.HasMany(x => x.Children).WithOne(x => x.Parent).HasForeignKey(x => x.ParentFolderId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FileRecord>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.OriginalFileName);
                b.HasOne<EncryptionKey>().WithMany().HasForeignKey(f => f.EncryptionKeyId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EncryptionKey>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.KeyVersion);
            });

            modelBuilder.Entity<AuditLog>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.CreatedAt);
                b.Property(x => x.Details).HasMaxLength(2000);
            });

            modelBuilder.Entity<AdminOtp>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.EmailOrMobile);
                b.HasIndex(x => x.ExpiresAt);
            });
        }
    }
}
