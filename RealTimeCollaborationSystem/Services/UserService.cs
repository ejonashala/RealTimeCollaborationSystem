using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.Models.Dtos;
using RealTimeCollaborationSystem.Services.Interfaces;
using System;

namespace RealTimeCollaborationSystem.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> EmailExists(string email)
        {
            return await _db.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User?> LoginAsync(UserLoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, dto.Password);

            return result == PasswordVerificationResult.Failed ? null : user;
        }

        public async Task<User> CreateUser(RegisterDto dto)
        {
            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "Student" : dto.Role,
                PhotoUrl = "/images/users/default.png",
                Language = "sq"
            };

            user.Password = _passwordHasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return user;
        }
    }
}
