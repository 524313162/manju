// @name:         ProjectDbContext
// @author:       AI Assistant
// @namespace:    ManjuCraft.Infrastructure
// @description:  项目数据库上下文实现
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Infrastructure
{
    /// <summary>
    /// 项目数据库上下文实现
    /// </summary>
    public class ProjectDbContext : DbContext, IProjectDbContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">数据库上下文选项</param>
        public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 项目集合
        /// </summary>
        public DbSet<Project> Projects => Set<Project>();

        /// <summary>
        /// 故事集合
        /// </summary>
        public DbSet<Story> Stories => Set<Story>();

        /// <summary>
        /// 演员集合
        /// </summary>
        public DbSet<Actor> Actors => Set<Actor>();

        /// <summary>
        /// 道具集合
        /// </summary>
        public DbSet<Prop> Props => Set<Prop>();

        /// <summary>
        /// 场景集合
        /// </summary>
        public DbSet<Scene> Scenes => Set<Scene>();

        /// <summary>
        /// 技能集合
        /// </summary>
        public DbSet<Skill> Skills => Set<Skill>();

        /// <summary>
        /// BGM集合
        /// </summary>
        public DbSet<Bgm> Bgms => Set<Bgm>();

        /// <summary>
        /// 分集集合
        /// </summary>
        public DbSet<Episode> Episodes => Set<Episode>();

        /// <summary>
        /// 分镜集合
        /// </summary>
        public DbSet<Shot> Shots => Set<Shot>();

        /// <summary>
        /// 实体图片集合
        /// </summary>
        public DbSet<EntityImage> EntityImages => Set<EntityImage>();

        /// <summary>
        /// 工作流集合
        /// </summary>
        public DbSet<Workflow> Workflows => Set<Workflow>();

        /// <summary>
        /// 配置模型元数据
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Project relationships
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Stories)
                .WithOne(s => s.Project)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Actors)
                .WithOne(a => a.Project)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Props)
                .WithOne(p => p.Project)
                .HasForeignKey(p => p.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Scenes)
                .WithOne(s => s.Project)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Skills)
                .WithOne(s => s.Project)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Bgms)
                .WithOne(b => b.Project)
                .HasForeignKey(b => b.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Episodes)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.EntityImages)
                .WithOne(ei => ei.Project)
                .HasForeignKey(ei => ei.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Workflows)
                .WithOne(w => w.Project)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // Episode -> Shot
            modelBuilder.Entity<Episode>()
                .HasMany(e => e.Shots)
                .WithOne(s => s.Episode)
                .HasForeignKey(s => s.EpisodeId)
                .OnDelete(DeleteBehavior.Cascade);

            // EntityImage - no self-referential relationship
            // EntityType + EntityId + ViewType uniquely identifies an asset

            // Unique project name
            modelBuilder.Entity<Project>()
                .HasIndex(p => p.Name)
                .IsUnique();

            // String length constraints
            modelBuilder.Entity<Project>()
                .Property(p => p.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Actor>()
                .Property(a => a.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Prop>()
                .Property(p => p.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Scene>()
                .Property(s => s.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Skill>()
                .Property(s => s.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Bgm>()
                .Property(b => b.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Episode>()
                .Property(e => e.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Workflow>()
                .Property(w => w.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Workflow>()
                .Property(w => w.WorkflowType)
                .HasMaxLength(50);

            modelBuilder.Entity<Actor>()
                .Property(a => a.DefaultWorkflowType)
                .HasMaxLength(50);

            modelBuilder.Entity<Prop>()
                .Property(p => p.DefaultWorkflowType)
                .HasMaxLength(50);

            modelBuilder.Entity<Scene>()
                .Property(s => s.DefaultWorkflowType)
                .HasMaxLength(50);

            modelBuilder.Entity<Skill>()
                .Property(s => s.DefaultWorkflowType)
                .HasMaxLength(50);

            modelBuilder.Entity<Bgm>()
                .Property(b => b.DefaultWorkflowType)
                .HasMaxLength(50);

            modelBuilder.Entity<Shot>()
                .Property(s => s.FirstFrameWorkflowType)
                .HasMaxLength(50);

            modelBuilder.Entity<Shot>()
                .Property(s => s.VideoWorkflowType)
                .HasMaxLength(50);

            modelBuilder.Entity<EntityImage>()
                .Property(ei => ei.EntityType)
                .HasMaxLength(50);

            modelBuilder.Entity<EntityImage>()
                .Property(ei => ei.ViewType)
                .HasMaxLength(50);

            modelBuilder.Entity<EntityImage>()
                .Property(ei => ei.MediaType)
                .HasMaxLength(10);

            modelBuilder.Entity<EntityImage>()
                .Property(ei => ei.FilePath)
                .HasMaxLength(1024);

            base.OnModelCreating(modelBuilder);
        }
    }
}
