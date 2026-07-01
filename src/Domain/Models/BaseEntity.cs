// @name:         BaseEntity
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  基础实体类
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    /// <summary>
    /// 基础实体基类
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// 创建时间（Unix时间戳毫秒）
        /// </summary>
        public long CreatedTime { get; set; }

        /// <summary>
        /// 更新时间（Unix时间戳毫秒）
        /// </summary>
        public long UpdatedTime { get; set; }
    }
}
