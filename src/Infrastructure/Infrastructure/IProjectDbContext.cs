using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Infrastructure
{
    public interface IProjectDbContext : IDisposable
    {
        DbSet<Project> Projects { get; }
        DbSet<Story> Stories { get; }
        DbSet<StoryChapter> StoryChapters { get; }
        DbSet<Asset> Assets { get; }
        DbSet<Resource> Resources { get; }
        DbSet<Episode> Episodes { get; }
        DbSet<Shot> Shots { get; }
        DbSet<ShotFrame> ShotFrames { get; }
        DbSet<Workflow> Workflows { get; }
        DbSet<PromptTemplate> PromptTemplates { get; }
        DbSet<ApiProvider> ApiProviders { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}