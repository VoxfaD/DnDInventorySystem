using System.Collections.Generic;
using System.Security.Claims;
using DnDInventorySystem.Data;
using DnDInventorySystem.Models;
using DnDInventorySystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DnDInventorySystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index", "Games");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Incorrect email address or password!");
                return View(model);
            }

            PasswordVerificationResult passwordResult;
            try
            {
                passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            }
            catch (FormatException)
            {
                // Seeded users had plain-text passwords; fallback to plain comparison and upgrade to a hash.
                if (string.Equals(user.PasswordHash, model.Password, StringComparison.Ordinal))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    passwordResult = PasswordVerificationResult.Success;
                }
                else
                {
                    passwordResult = PasswordVerificationResult.Failed;
                }
            }

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Incorrect email address or password!");
                return View(model);
            }

            await SignInUserAsync(user);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Games");
        }

        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index", "Games");
            }

            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState[nameof(model.Name)]?.Errors.Clear();
                ModelState.AddModelError(nameof(model.Name), "Username is required!");
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState[nameof(model.Email)]?.Errors.Clear();
                ModelState.AddModelError(nameof(model.Email), "E-mail is required!");
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState[nameof(model.Password)]?.Errors.Clear();
                ModelState.AddModelError(nameof(model.Password), "Password is required!");
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                ModelState[nameof(model.ConfirmPassword)]?.Errors.Clear();
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Password confirmation is required!");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var trimmedEmail = model.Email.Trim();
            var trimmedName = model.Name.Trim();

            var emailExists = await _context.Users.AnyAsync(u => u.Email == trimmedEmail);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "This email address is already registered!");
                return View(model);
            }

            var userExists = await _context.Users.AnyAsync(u => u.Name == trimmedName);
            if (userExists)
            {
                ModelState.AddModelError(nameof(model.Name), "Username already exists!");
                return View(model);
            }

            var user = new User
            {
                Name = trimmedName,
                Email = trimmedEmail
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await SignInUserAsync(user);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Games");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var vm = new UpdateProfileViewModel
            {
                DisplayName = user.Name,
                Email = user.Email
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UpdateProfileViewModel model)
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var wantsPasswordChange = !string.IsNullOrWhiteSpace(model.NewPassword);
            if (wantsPasswordChange && string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Enter your current password to set a new one.");
                return View(model);
            }

            if (wantsPasswordChange)
            {
                const string passwordPattern = @"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,128}$";
                if (model.NewPassword!.Length < 8 || model.NewPassword.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(model.NewPassword, passwordPattern))
                {
                    ModelState.AddModelError(nameof(model.NewPassword), "Password does not meet security requirements! Must contain a number and a unique symbol, such as #, must be at least an uppercase letter and the number of symbols is 8-128 symbols.");
                    return View(model);
                }

                if (!await VerifyPasswordAsync(user, model.CurrentPassword!))
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
                    return View(model);
                }

                user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword!);
            }

            var trimmedDisplayName = model.DisplayName.Trim();
            var nameExists = await _context.Users
                .AnyAsync(u => u.Id != user.Id && u.Name == trimmedDisplayName);
            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.DisplayName), "Username already exists!");
                return View(model);
            }

            user.Name = model.DisplayName.Trim();
            user.Email = model.Email.Trim();
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await SignInUserAsync(user); // refresh claims with updated name
            TempData["ProfileMessage"] = "Profile updated.";
            return RedirectToAction(nameof(Profile));
        }

        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        private int GetCurrentUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue))
            {
                throw new InvalidOperationException("User identifier claim is missing.");
            }

            return int.Parse(claimValue);
        }

        private async Task<bool> VerifyPasswordAsync(User user, string password)
        {
            PasswordVerificationResult result;
            try
            {
                result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }
            catch (FormatException)
            {
                result = string.Equals(user.PasswordHash, password, StringComparison.Ordinal)
                    ? PasswordVerificationResult.Success
                    : PasswordVerificationResult.Failed;
            }

            return result != PasswordVerificationResult.Failed;
        }
    }
}
