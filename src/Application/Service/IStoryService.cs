using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    public interface IStoryService
    {
        Task<List<Story>> GetByProjectIdAsync(long projectId);

        Task<Story> GetByIdAsync(long id);

        Task<Story> CreateAsync(Story story);

        Task<Story> UpdateAsync(Story story);

        Task DeleteAsync(long id);
    }
}