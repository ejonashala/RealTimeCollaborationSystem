using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.Models.Dtos;
using RealTimeCollaborationSystem.Services.Interfaces;
using System;

namespace RealTimeCollaborationSystem.Controllers
{
    public class AccountController : Controller
    {
        private const string DefaultAvatarUrl = "/images/users/default-avatar.svg";
        private readonly IUserService _userService;
        private readonly AppDbContext _db;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AccountController(IUserService userService, AppDbContext db)
        {
            _userService = userService;
            _db = db;
        }

        // ================= LOGIN =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Incorrect email or password.");
                return View(dto);
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.Password,
                dto.Password
            );

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Incorrect email or password.");
                return View(dto);
            }

            SetUserSession(user);

            return RedirectToAction("Index", "Dashboard");
        }

        // ================= REGISTER =================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (await _userService.EmailExists(dto.Email))
            {
                ModelState.AddModelError("Email", "This email already exists.");
                return View(dto);
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Role = "Student", 
                PhotoUrl = "/images/users/default.png",
                Language = "sq"
            };

            user.Password = _passwordHasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            SetUserSession(user);

            return RedirectToAction("Index", "Dashboard");
        }

        // ================= LOGOUT =================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ================= CHANGE PASSWORD =================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login");

            var user = await _db.Users.FindAsync(int.Parse(userIdStr));

            if (user == null)
                return RedirectToAction("Login");

            var verify = _passwordHasher.VerifyHashedPassword(
                user,
                user.Password,
                dto.CurrentPassword
            );

            if (verify == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "The current password is incorrect.";
                return View();
            }

            user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);
            await _db.SaveChangesAsync();

            ViewBag.Success = "Password changed successfully.";
            return View();
        }

        // ================= FORGOT PASSWORD =================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return RedirectToAction("ResetPassword");

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

            await _db.SaveChangesAsync();

            return RedirectToAction("ResetPassword", new { token = user.PasswordResetToken });
        }

        // ================= RESET PASSWORD =================

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow
            );

            if (user == null)
                return RedirectToAction("Login");

            return View(new ResetPasswordDto { Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == dto.Token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow
            );

            if (user == null)
            {
                ModelState.AddModelError("", "This reset link is invalid or has expired.");
                return View(dto);
            }

            user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _db.SaveChangesAsync();

            SetUserSession(user);

            return RedirectToAction("Index", "Dashboard");
        }

        // ================= HELPER =================

        private void SetUserSession(User user)
        {
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role ?? "Student");
            HttpContext.Session.SetString(
                "PhotoUrl",
                ResolveSessionPhotoUrl(user)
            );
            HttpContext.Session.SetString(
                "Language",
                NormalizeLanguage(user.Language)
            );
        }

        private static string NormalizeLanguage(string? language)
        {
            if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                return "en";
            }

            return "sq";
        }

        private static string ResolveSessionPhotoUrl(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.ProfilePhotoUrl))
            {
                return user.ProfilePhotoUrl;
            }

            if (!string.IsNullOrWhiteSpace(user.PhotoUrl)
                && !string.Equals(user.PhotoUrl, "/images/users/default.png", StringComparison.OrdinalIgnoreCase))
            {
                return user.PhotoUrl;
            }

            return DefaultAvatarUrl;
        }
    }
}
