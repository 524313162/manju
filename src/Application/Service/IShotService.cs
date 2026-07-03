using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    public interface IShotService
    {
        Task<List<Shot>> GetByEpisodeAsync(long episodeId);

        Task<Shot> GetByIdAsync(long id);

        Task<Shot> CreateAsync(Shot shot);

        Task<Shot> UpdateAsync(Shot shot);

        Task DeleteAsync(long id);
    }
}