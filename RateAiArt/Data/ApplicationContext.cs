using Microsoft.EntityFrameworkCore;

using RateAiArt.Data.Models;

namespace RateAiArt.Data
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
        }

        public DbSet<ArtModel> Arts { get; set; }

        public DbSet<ArtPublisherModel> Publishers { get; set; }

        public DbSet<PublisherLeaderBoardScoreModel> PublisherLeaderBoardScores { get; set; }

        public DbSet<ArtRateResultModel> ArtRateResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ArtPublisherModel>()
                .HasMany(p => p.Arts)
                .WithOne(a => a.Publisher)
                .HasForeignKey(a => a.PublisherId);

            modelBuilder.Entity<ArtModel>()
                .HasOne(a => a.ArtRateResultModel)
                .WithOne(r => r.Art)
                .HasForeignKey<ArtRateResultModel>(r => r.ArtId);

            modelBuilder.Entity<ArtPublisherModel>()
                .HasMany(p => p.LeaderBoardScores)
                .WithOne(s => s.Publisher)
                .HasForeignKey(s => s.PublisherId);
        }
    }
}
