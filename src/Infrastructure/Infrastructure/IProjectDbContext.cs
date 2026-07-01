// @name:         IProjectDbContext
// @author:       AI Assistant
// @namespace:    ManjuCraft.Infrastructure
// @description:  项目数据库上下文接口
// @version:      1.0
// @date:         2026-06-30

using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Infrastructure
{
    /// <summary>
    /// 项目数据库上下文接口
    /// </summary>
    public interface IProjectDbContext : IDisposable
    {
        /// <summary>
        /// 项目集合
        /// </summary>
        DbSet<Project> Projects { get; }

        /// <summary>
        /// 故事集合
        /// </summary>
        DbSet<Story> Stories { get; }

        /// <summary>
        /// 演员集合
        /// </summary>
        DbSet<Actor> Actors { get; }

        /// <summary>
        /// 道具集合
        /// </summary>
        DbSet<Prop> Props { get; }

        /// <summary>
        /// 场景集合
        /// </summary>
        DbSet<Scene> Scenes { get; }

        /// <summary>
        /// 技能集合
        /// </summary>
        DbSet<Skill> Skills { get; }

        /// <summary>
        /// BGM集合
        /// </summary>
        DbSet<Bgm> Bgms { get; }

        /// <summary>
        /// 分集集合
        /// </summary>
        DbSet<Episode> Episodes { get; }

        /// <summary>
        /// 分镜集合
        /// </summary>
        DbSet<Shot> Shots { get; }

        /// <summary>
        /// 实体图片集合
        /// </summary>
        DbSet<EntityImage> EntityImages { get; }

        /// <summary>
        /// 工作流集合
        /// </summary>
        DbSet<Workflow> Workflows { get; }

        /// <summary>
        /// 异步保存更改
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
