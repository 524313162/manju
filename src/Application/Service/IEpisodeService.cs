using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    public interface IEpisodeService
    {
        Task<List<Episode>> GetByProjectAsync(long projectId);

        Task<Episode> GetByIdAsync(long id);

        Task<Episode> CreateAsync(Episode episode);

        Task<Episode> UpdateAsync(Episode episode);

        Task DeleteAsync(long id);

        Task UpdateOrderAsync(long id, int newOrder);
    }
}