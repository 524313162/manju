using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Infrastructure
{
    public class ProjectDbContext : DbContext, IProjectDbContext
    {
        public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options)
        {
        }

        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Story> Stories => Set<Story>();
        public DbSet<StoryChapter> StoryChapters => Set<StoryChapter>();
        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<Episode> Episodes => Set<Episode>();
        public DbSet<Shot> Shots => Set<Shot>();
        public DbSet<ShotFrame> ShotFrames => Set<ShotFrame>();
        public DbSet<ShotFrameAsset> ShotFrameAssets => Set<ShotFrameAsset>();
        public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
        public DbSet<ApiProvider> ApiProviders => Set<ApiProvider>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Project relationships
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Stories)
                .WithOne(s => s.Project)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Assets)
                .WithOne(a => a.Project)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Episodes)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.ShotFrames)
                .WithOne(sf => sf.Project)
                .HasForeignKey(sf => sf.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Story -> StoryChapter
            modelBuilder.Entity<Story>()
                .HasMany(s => s.Chapters)
                .WithOne(c => c.Story)
                .HasForeignKey(c => c.StoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Episode -> Shot
            modelBuilder.Entity<Episode>()
                .HasMany(e => e.Shots)
                .WithOne(s => s.Episode)
                .HasForeignKey(s => s.EpisodeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Episode -> StoryChapter (optional)
            modelBuilder.Entity<Episode>()
                .HasOne(e => e.StoryChapter)
                .WithMany()
                .HasForeignKey(e => e.StoryChapterId)
                .OnDelete(DeleteBehavior.SetNull);

            // Shot -> ShotFrame
            modelBuilder.Entity<Shot>()
                .HasMany(s => s.Frames)
                .WithOne(f => f.Shot)
                .HasForeignKey(f => f.ShotId)
                .OnDelete(DeleteBehavior.Cascade);

            // ShotFrame -> ShotFrameAsset
            modelBuilder.Entity<ShotFrame>()
                .HasMany(sf => sf.ShotFrameAssets)
                .WithOne(sfa => sfa.ShotFrame)
                .HasForeignKey(sfa => sfa.ShotFrameId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShotFrameAsset>()
                .HasOne(sfa => sfa.Asset)
                .WithMany()
                .HasForeignKey(sfa => sfa.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShotFrameAsset>()
                .HasIndex(sfa => new { sfa.ShotFrameId, sfa.AssetId })
                .IsUnique();

            // Asset self-reference (ParentId for variants)
            modelBuilder.Entity<Asset>()
                .HasOne(a => a.Parent)
                .WithMany(a => a.Children)
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Asset -> Resource
            modelBuilder.Entity<Asset>()
                .HasOne(a => a.Resource)
                .WithMany()
                .HasForeignKey(a => a.ResourceId)
                .OnDelete(DeleteBehavior.SetNull);

            // ShotFrame -> Resource
            modelBuilder.Entity<ShotFrame>()
                .HasOne(sf => sf.Resource)
                .WithMany()
                .HasForeignKey(sf => sf.ResourceId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraints
            modelBuilder.Entity<Project>()
                .HasIndex(p => p.Name)
                .IsUnique();

            // String length constraints
            modelBuilder.Entity<Project>()
                .Property(p => p.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Story>()
                .Property(s => s.Title)
                .HasMaxLength(512);

            modelBuilder.Entity<StoryChapter>()
                .Property(c => c.ChapterName)
                .HasMaxLength(256);

            modelBuilder.Entity<Asset>()
                .Property(a => a.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Resource>()
                .Property(r => r.MediaType)
                .HasMaxLength(16);

            modelBuilder.Entity<Resource>()
                .Property(r => r.FilePath)
                .HasMaxLength(1024);

            modelBuilder.Entity<Shot>()
                .Property(s => s.ShotSize)
                .HasMaxLength(32);

            modelBuilder.Entity<Shot>()
                .Property(s => s.CameraMovement)
                .HasMaxLength(64);

            modelBuilder.Entity<ShotFrame>()
                .Property(f => f.FrameType)
                .HasMaxLength(32);

            modelBuilder.Entity<Episode>()
                .Property(e => e.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<PromptTemplate>()
                .Property(pt => pt.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<PromptTemplate>()
                .Property(pt => pt.TemplateType)
                .HasMaxLength(64);

            modelBuilder.Entity<ApiProvider>()
                .Property(ap => ap.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<ApiProvider>()
                .Property(ap => ap.ApiUrl)
                .HasMaxLength(512);

            modelBuilder.Entity<ApiProvider>()
                .Property(ap => ap.ApiKey)
                .HasMaxLength(1024);

            base.OnModelCreating(modelBuilder);
        }
    }
}