// @name:         GlobalSearchService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  全局搜索服务
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 全局搜索服务接口
    /// </summary>
    public interface IGlobalSearchService
    {
        /// <summary>
        /// 执行搜索
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="ct">取消令牌</param>
        Task<SearchResult> SearchAsync(string keyword, long? projectId = null, CancellationToken ct = default);
    }

    /// <summary>
    /// 搜索结果
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// 项目结果
        /// </summary>
        public List<ProjectResult> Projects { get; set; } = new();

        /// <summary>
        /// 故事结果
        /// </summary>
        public List<StoryResult> Stories { get; set; } = new();

        /// <summary>
        /// 演员结果
        /// </summary>
        public List<AssetResult> Actors { get; set; } = new();

        /// <summary>
        /// 道具结果
        /// </summary>
        public List<AssetResult> Props { get; set; } = new();

        /// <summary>
        /// 场景结果
        /// </summary>
        public List<AssetResult> Scenes { get; set; } = new();

        /// <summary>
        /// 技能结果
        /// </summary>
        public List<AssetResult> Skills { get; set; } = new();

        /// <summary>
        /// BGM结果
        /// </summary>
        public List<AssetResult> Bgms { get; set; } = new();

        /// <summary>
        /// 分集结果
        /// </summary>
        public List<EpisodeResult> Episodes { get; set; } = new();

        /// <summary>
        /// 分镜结果
        /// </summary>
        public List<ShotResult> Shots { get; set; } = new();
    }

    /// <summary>
    /// 项目搜索结果
    /// </summary>
    public class ProjectResult { public long Id { get; set; } public string Name { get; set; } = ""; }

    /// <summary>
    /// 故事搜索结果
    /// </summary>
    public class StoryResult { public long Id { get; set; } public string Content { get; set; } = ""; }

    /// <summary>
    /// 资产搜索结果
    /// </summary>
    public class AssetResult { public long Id { get; set; } public string Name { get; set; } = ""; public string Type { get; set; } = ""; }

    /// <summary>
    /// 分集搜索结果
    /// </summary>
    public class EpisodeResult { public long Id { get; set; } public string Name { get; set; } = ""; }

    /// <summary>
    /// 分镜搜索结果
    /// </summary>
    public class ShotResult { public long Id { get; set; } public string Dialog { get; set; } = ""; }

    /// <summary>
    /// 全局搜索服务实现
    /// </summary>
    public class GlobalSearchService : IGlobalSearchService
    {
        private readonly IProjectDbContext _db;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">数据库上下文</param>
        public GlobalSearchService(IProjectDbContext db) => _db = db;

        /// <summary>
        /// 执行搜索
        /// </summary>
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
                    .Where(s => s.ProjectId == pid && s.Content.ToLower().Contains(keywordLower))
                    .Select(s => new StoryResult { Id = s.Id, Content = s.Content }).ToListAsync(ct);

                results.Actors = await _db.Actors.Where(a => a.ProjectId == pid && a.Name.ToLower().Contains(keywordLower))
                    .Select(a => new AssetResult { Id = a.Id, Name = a.Name, Type = "Actor" }).ToListAsync(ct);
                results.Props = await _db.Props.Where(p => p.ProjectId == pid && p.Name.ToLower().Contains(keywordLower))
                    .Select(p => new AssetResult { Id = p.Id, Name = p.Name, Type = "Prop" }).ToListAsync(ct);
                results.Scenes = await _db.Scenes.Where(s => s.ProjectId == pid && s.Name.ToLower().Contains(keywordLower))
                    .Select(s => new AssetResult { Id = s.Id, Name = s.Name, Type = "Scene" }).ToListAsync(ct);
                results.Skills = await _db.Skills.Where(s => s.ProjectId == pid && s.Name.ToLower().Contains(keywordLower))
                    .Select(s => new AssetResult { Id = s.Id, Name = s.Name, Type = "Skill" }).ToListAsync(ct);
                results.Bgms = await _db.Bgms.Where(b => b.ProjectId == pid && b.Name.ToLower().Contains(keywordLower))
                    .Select(b => new AssetResult { Id = b.Id, Name = b.Name, Type = "Bgm" }).ToListAsync(ct);
                results.Episodes = await _db.Episodes.Where(e => e.ProjectId == pid && e.Name.ToLower().Contains(keywordLower))
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
