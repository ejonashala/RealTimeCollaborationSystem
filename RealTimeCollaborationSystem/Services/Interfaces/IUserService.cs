using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.Models.Dtos;
using System.Threading.Tasks;

namespace RealTimeCollaborationSystem.Services.Interfaces
{
    public interface IUserService
    {
        Task<bool> EmailExists(string email);
        Task<User> CreateUser(RegisterDto dto);
        Task<User?> LoginAsync(UserLoginDto dto);
    }
}
