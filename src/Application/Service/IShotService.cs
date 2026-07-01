// @name:         IShotService
// @author:       AI Assistant
// @namespace:    ManjuCraft.Application.Service
// @description:  分镜服务接口
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;
using ManjuCraft.Infrastructure;

namespace ManjuCraft.Application.Service
{
    /// <summary>
    /// 分镜服务接口
    /// </summary>
    public interface IShotService
    {
        /// <summary>
        /// 根据分集获取分镜列表
        /// </summary>
        /// <param name="episodeId">分集ID</param>
        Task<List<Shot>> GetByEpisodeAsync(long episodeId);

        /// <summary>
        /// 根据ID获取分镜
        /// </summary>
        /// <param name="id">分镜ID</param>
        Task<Shot> GetByIdAsync(long id);

        /// <summary>
        /// 创建分镜
        /// </summary>
        /// <param name="shot">分镜实体</param>
        Task<Shot> CreateAsync(Shot shot);

        /// <summary>
        /// 更新分镜
        /// </summary>
        /// <param name="shot">分镜实体</param>
        Task<Shot> UpdateAsync(Shot shot);

        /// <summary>
        /// 删除分镜
        /// </summary>
        /// <param name="id">分镜ID</param>
        Task DeleteAsync(long id);
    }

    /// <summary>
    /// 分镜服务实现
    /// </summary>
    public class ShotService : IShotService
    {
        private readonly IProjectDbContext _db;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">数据库上下文</param>
        public ShotService(IProjectDbContext db) => _db = db;

        /// <summary>
        /// 根据分集获取分镜列表
        /// </summary>
        /// <param name="episodeId">分集ID</param>
        public async Task<List<Shot>> GetByEpisodeAsync(long episodeId)
            => await _db.Shots.Where(s => s.EpisodeId == episodeId).OrderBy(s => s.Order).ToListAsync();

        /// <summary>
        /// 根据ID获取分镜
        /// </summary>
        /// <param name="id">分镜ID</param>
        public async Task<Shot> GetByIdAsync(long id)
            => await _db.Shots.FindAsync(id) ?? new Shot();

        /// <summary>
        /// 创建分镜
        /// </summary>
        /// <param name="shot">分镜实体</param>
        public async Task<Shot> CreateAsync(Shot shot)
        {
            shot.CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            shot.UpdatedTime = shot.CreatedTime;
            await _db.Shots.AddAsync(shot);
            await _db.SaveChangesAsync();
            return shot;
        }

        /// <summary>
        /// 更新分镜
        /// </summary>
        /// <param name="shot">分镜实体</param>
        public async Task<Shot> UpdateAsync(Shot shot)
        {
            var existing = await _db.Shots.FindAsync(shot.Id);
            if (existing == null) return new Shot();
            existing.FirstFramePrompt = shot.FirstFramePrompt;
            existing.FirstFrameWorkflowType = shot.FirstFrameWorkflowType;
            existing.Dialog = shot.Dialog;
            existing.VideoPrompt = shot.VideoPrompt;
            existing.VideoWorkflowType = shot.VideoWorkflowType;
            existing.Order = shot.Order;
            existing.UpdatedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();
            return existing;
        }

        /// <summary>
        /// 删除分镜
        /// </summary>
        /// <param name="id">分镜ID</param>
        public async Task DeleteAsync(long id)
        {
            var shot = await _db.Shots.FindAsync(id);
            if (shot != null) { _db.Shots.Remove(shot); await _db.SaveChangesAsync(); }
        }
    }
}
