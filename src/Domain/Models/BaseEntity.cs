// @name:         BaseEntity
// @author:       AI Assistant
// @namespace:    ManjuCraft.Domain.Models
// @description:  基础实体类
// @version:      1.0
// @date:         2026-06-30

using System.ComponentModel.DataAnnotations;

namespace ManjuCraft.Domain.Models
{
    public abstract class BaseEntity<TKey>
    {
        [Key]
        public TKey Id { get; set; } = default!;

        public long CreatedTime { get; set; }

        public long UpdatedTime { get; set; }
    }

    public abstract class BaseEntity : BaseEntity<long>
    {
    }
}
