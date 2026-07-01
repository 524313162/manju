// @name:         IEpisodeService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  分集服务接口
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 分集服务接口
    /// </summary>
    public interface IEpisodeService
    {
        /// <summary>
        /// 根据项目获取分集列表
        /// </summary>
        /// <param name="projectId">项目ID</param>
        Task<List<Episode>> GetByProjectAsync(long projectId);

        /// <summary>
        /// 根据ID获取分集
        /// </summary>
        /// <param name="id">分集ID</param>
        Task<Episode> GetByIdAsync(long id);

        /// <summary>
        /// 创建分集
        /// </summary>
        /// <param name="episode">分集实体</param>
        Task<Episode> CreateAsync(Episode episode);

        /// <summary>
        /// 更新分集
        /// </summary>
        /// <param name="episode">分集实体</param>
        Task<Episode> UpdateAsync(Episode episode);

        /// <summary>
        /// 更新排序
        /// </summary>
        /// <param name="orderId">分集ID</param>
        /// <param name="newOrder">新排序值</param>
        Task UpdateOrderAsync(long orderId, int newOrder);
    }

    /// <summary>
    /// 分集服务实现
    /// </summary>
    public class EpisodeService : IEpisodeService
    {
        private readonly IProjectDbContext _db;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">数据库上下文</param>
        public EpisodeService(IProjectDbContext db) => _db = db;

        /// <summary>
        /// 根据项目获取分集列表
        /// </summary>
        /// <param name="projectId">项目ID</param>
        public async Task<List<Episode>> GetByProjectAsync(long projectId)
            => await _db.Episodes.Where(e => e.ProjectId == projectId).OrderBy(e => e.Order).ToListAsync();

        /// <summary>
        /// 根据ID获取分集
        /// </summary>
        /// <param name="id">分集ID</param>
        public async Task<Episode> GetByIdAsync(long id)
            => await _db.Episodes.FindAsync(id) ?? new Episode();

        /// <summary>
        /// 创建分集
        /// </summary>
        /// <param name="ep">分集实体</param>
        public async Task<Episode> CreateAsync(Episode ep)
        {
            ep.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ep.UpdatedTime = ep.CreatedTime;
            await _db.Episodes.AddAsync(ep);
            await _db.SaveChangesAsync();
            return ep;
        }

        /// <summary>
        /// 更新分集
        /// </summary>
        /// <param name="ep">分集实体</param>
        public async Task<Episode> UpdateAsync(Episode ep)
        {
            var existing = await _db.Episodes.FindAsync(ep.Id);
            if (existing == null) return new Episode();
            existing.Name = ep.Name;
            existing.Duration = ep.Duration;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();
            return existing;
        }

        /// <summary>
        /// 更新排序
        /// </summary>
        /// <param name="orderId">分集ID</param>
        /// <param name="newOrder">新排序值</param>
        public async Task UpdateOrderAsync(long orderId, int newOrder)
        {
            var ep = await _db.Episodes.FindAsync(orderId);
            if (ep != null)
            {
                ep.Order = newOrder;
                ep.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await _db.SaveChangesAsync();
            }
        }
    }
}
