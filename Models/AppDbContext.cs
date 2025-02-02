using Microsoft.EntityFrameworkCore;

namespace HandshakesByDC_BEAssignment.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Carpark> Carparks { get; set; }
        public DbSet<CarparkImportLog> CarparkImportLogs { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserFavorite>()
                .HasKey(uf => new { uf.UserId, uf.CarparkNo });

            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.User)
                .WithOne(u => u.Favorite)
                .HasForeignKey<UserFavorite>(uf => uf.UserId);

            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.Carpark)
                .WithMany(c => c.FavoritedBy)
                .HasForeignKey(uf => uf.CarparkNo);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
