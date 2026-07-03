using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    public interface IShotFrameService
    {
        Task<List<ShotFrame>> GetByShotAsync(long shotId);

        Task<ShotFrame> GetByIdAsync(long id);

        Task<ShotFrame> CreateAsync(ShotFrame frame);

        Task<ShotFrame> UpdateAsync(ShotFrame frame);

        Task DeleteAsync(long id);
    }
}