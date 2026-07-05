using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ManjuCraft.Domain.Models
{
    public static class EnumExtensions
    {
        public static string DisplayName(this Enum value)
        {
            var attr = value.GetType()
                .GetField(value.ToString())
                ?.GetCustomAttribute<DisplayAttribute>();
            return attr?.Name ?? value.ToString();
        }
    }
}
