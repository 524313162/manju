using Microsoft.EntityFrameworkCore;
using ManjuCraft.Domain.Models;

namespace ManjuCraft.Application.Service
{
    public interface IProjectService
    {
        Task<List<Project>> GetAllAsync();

        Task<Project> GetByIdAsync(long id);

        Task<Project> CreateAsync(Project project);

        Task<Project> UpdateAsync(Project project);

        Task DeleteAsync(long id);
    }
}