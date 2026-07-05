using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    public interface IGlobalSearchService
    {
        Task<SearchResult> SearchAsync(string keyword, long? projectId = null, CancellationToken ct = default);
    }

    public class SearchResult
    {
        public List<ProjectResult> Projects { get; set; } = new();
        public List<StoryResult> Stories { get; set; } = new();
        public List<AssetResult> Assets { get; set; } = new();
        public List<StoryChapterResult> StoryChapters { get; set; } = new();
        public List<EpisodeResult> Episodes { get; set; } = new();
        public List<ShotResult> Shots { get; set; } = new();
    }

    public class ProjectResult { public long Id { get; set; } public string Name { get; set; } = ""; }
    public class StoryResult { public long Id { get; set; } public string Title { get; set; } = ""; }
    public class AssetResult { public long Id { get; set; } public string Name { get; set; } = ""; public AssetTypeEnum AssetType { get; set; } }
    public class StoryChapterResult { public long Id { get; set; } public string ChapterName { get; set; } = ""; public string Content { get; set; } = ""; }
    public class EpisodeResult { public long Id { get; set; } public string Name { get; set; } = ""; }
    public class ShotResult { public long Id { get; set; } public string Description { get; set; } = ""; }

    public class GlobalSearchService : IGlobalSearchService
    {
        private readonly IProjectDbContext _db;

        public GlobalSearchService(IProjectDbContext db) => _db = db;

        public async Task<SearchResult> SearchAsync(string keyword, long? projectId = null, CancellationToken ct = default)
        {
            var results = new SearchResult();
            var keywordLower = keyword?.Trim().ToLower();
            if (string.IsNullOrEmpty(keywordLower) || keywordLower.Length < 1) return results;

            if (projectId.HasValue && projectId.Value > 0)
            {
                var pid = projectId.Value;

                results.Projects = await _db.Projects
                    .Where(p => p.Id == pid && p.Name.ToLower().Contains(keywordLower))
                    .Select(p => new ProjectResult { Id = p.Id, Name = p.Name }).ToListAsync(ct);

                results.Stories = await _db.Stories
                    .Where(s => s.ProjectId == pid && s.Title.ToLower().Contains(keywordLower))
                    .Select(s => new StoryResult { Id = s.Id, Title = s.Title }).ToListAsync(ct);

                results.Assets = await _db.Assets
                    .Where(a => a.ProjectId == pid && a.Name.ToLower().Contains(keywordLower))
                    .Select(a => new AssetResult { Id = a.Id, Name = a.Name, AssetType = a.AssetType }).ToListAsync(ct);

                results.StoryChapters = await _db.StoryChapters
                    .Where(sc => sc.Story.ProjectId == pid && (sc.ChapterName.ToLower().Contains(keywordLower) || sc.Content.ToLower().Contains(keywordLower)))
                    .Select(sc => new StoryChapterResult { Id = sc.Id, ChapterName = sc.ChapterName, Content = sc.Content }).ToListAsync(ct);

                results.Episodes = await _db.Episodes
                    .Where(e => e.ProjectId == pid && e.Name.ToLower().Contains(keywordLower))
                    .Select(e => new EpisodeResult { Id = e.Id, Name = e.Name }).ToListAsync(ct);
            }
            else
            {
                results.Projects = await _db.Projects
                    .Where(p => p.Name.ToLower().Contains(keywordLower))
                    .Select(p => new ProjectResult { Id = p.Id, Name = p.Name }).ToListAsync(ct);
            }
            return results;
        }
    }
}