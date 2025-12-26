using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DnDInventorySystem.Data;
using DnDInventorySystem.Models;
using DnDInventorySystem.ViewModels;
using DnDInventorySystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DnDInventorySystem.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HistoryLogService _historyLog;

        public GamesController(ApplicationDbContext context, HistoryLogService historyLog)
        {
            _context = context;
            _historyLog = historyLog;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var games = await QueryUserGames(userId)
                .Include(g => g.CreatedByUser)
                .OrderBy(g => g.Name)
                .ToListAsync();

            return View(games);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var game = await QueryUserGames(userId)
                .Include(g => g.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (game == null)
            {
                return NotFound();
            }

            var charactersQuery = _context.Characters
                .Include(c => c.Owner)
                .Where(c => c.GameId == game.Id)
                .OrderBy(c => c.Name);
            var isOwner = await IsOwnerAsync(game.Id);
            var privileges = await GetUserPrivilegesAsync(game.Id, isOwner);
            var itemsQuery = _context.Items
                .Include(i => i.Category)
                .Where(i => i.GameId == game.Id)
                .OrderBy(i => i.Name);
            var categoriesQuery = _context.Categories
                .Include(c => c.Items)
                .Where(c => c.GameId == game.Id)
                .OrderBy(c => c.Name);

            var viewModel = new GameDetailsViewModel
            {
                Game = game,
                Characters = await charactersQuery.Take(3).ToListAsync(),
                CharacterCount = await charactersQuery.CountAsync(),
                Items = await itemsQuery.Take(3).ToListAsync(),
                ItemCount = await itemsQuery.CountAsync(),
                Categories = await categoriesQuery.Take(3).ToListAsync(),
                CategoryCount = await categoriesQuery.CountAsync()
            };

            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(game.Id, isOwner);
            return View(viewModel);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Game game)
        {
            // if (!ModelState.IsValid)
            // {
            //     return View(game);
            // }

            var userId = GetCurrentUserId();
            game.CreatedByUserId = userId;
            game.CreatedAt = System.DateTime.UtcNow;

            game.UserGameRoles.Add(new UserGameRole
            {
                UserId = userId,
                IsOwner = true,
                Privileges = PrivilegeSets.Owner
            });

            _context.Games.Add(game);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "We couldn't save the game. " +
                    "Please verify the information and try again.");
                ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
                return View(game);
            }

            TempData["GameMessage"] = $"Game \"{game.Name}\" created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id && g.CreatedByUserId == userId);
            if (game == null)
            {
                return NotFound();
            }

            await SetHistorySidebarAsync(game.Id, true);
            return View(game);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Game updatedGame)
        {
            if (id != updatedGame.Id)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id && g.CreatedByUserId == userId);
            if (game == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await SetHistorySidebarAsync(updatedGame.Id, true);
                return View(updatedGame);
            }

            game.Name = updatedGame.Name;
            game.Description = updatedGame.Description;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var game = await _context.Games
                .Include(g => g.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id && m.CreatedByUserId == userId);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id && g.CreatedByUserId == userId);
            if (game != null)
            {
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Players(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await GetOwnedGameAsync(id.Value);
            if (game == null)
            {
                return NotFound();
            }

            var players = await _context.UserGameRoles
                .Include(rp => rp.User)
                .Where(rp => rp.GameId == game.Id)
                .OrderBy(rp => rp.User.Name)
                .ToListAsync();

            var allowedPrivileges = new[]
            {
                GamePrivilege.CreateCategories,
                GamePrivilege.EditCategories,
                GamePrivilege.DeleteCategories,
                GamePrivilege.ViewCategories,
                GamePrivilege.CreateItems,
                GamePrivilege.EditItems,
                GamePrivilege.DeleteItems,
                GamePrivilege.ViewItems,
                GamePrivilege.CreateCharacters,
                GamePrivilege.EditCharacters,
                GamePrivilege.DeleteCharacters,
                GamePrivilege.ViewCharacters,
                GamePrivilege.EditCharacterInventory,
                GamePrivilege.RemoveItemsFromCharacters,
                GamePrivilege.AddItemsToCharacters
            };

            var viewModel = new GamePlayersViewModel
            {
                Game = game,
                Players = players
            };

            ViewBag.AllowedPrivileges = allowedPrivileges;
            await SetHistorySidebarAsync(game.Id, true);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KickPlayer(int gameId, int userId)
        {
            var game = await GetOwnedGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            if (userId == game.CreatedByUserId)
            {
                TempData["GameMessage"] = "You cannot remove the game owner.";
                return RedirectToAction(nameof(Players), new { id = game.Id });
            }

            var rolePerson = await _context.UserGameRoles
                .FirstOrDefaultAsync(rp => rp.GameId == game.Id && rp.UserId == userId);

            if (rolePerson != null)
            {
                _context.UserGameRoles.Remove(rolePerson);
                await _context.SaveChangesAsync();
                var actor = await GetCurrentUserNameAsync();
                await LogAsync(game.Id, "UserRemoved", $"{actor} removed a player from the game", null, null, null);
                TempData["GameMessage"] = "Player removed from the game.";
            }

            return RedirectToAction(nameof(Players), new { id = game.Id });
        }

        public async Task<IActionResult> EditPlayerPrivileges(int gameId, int userId)
        {
            var game = await GetOwnedGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            if (userId == game.CreatedByUserId)
            {
                TempData["GameMessage"] = "You cannot change privileges for the game owner.";
                return RedirectToAction(nameof(Players), new { id = game.Id });
            }

            var role = await _context.UserGameRoles
                .Include(rp => rp.User)
                .FirstOrDefaultAsync(rp => rp.GameId == game.Id && rp.UserId == userId);
            if (role == null)
            {
                return NotFound();
            }

            var viewModel = new EditPlayerPrivilegesViewModel
            {
                GameId = game.Id,
                UserId = role.UserId,
                UserName = role.User?.Name ?? "Player",
                Privileges = role.Privileges,
                SelectedPrivileges = Enum.GetValues(typeof(GamePrivilege))
                    .Cast<GamePrivilege>()
                    .Where(p => role.Privileges.HasFlag(p) && p != GamePrivilege.None && p != GamePrivilege.All)
                    .ToList()
            };

            await SetHistorySidebarAsync(game.Id, true);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPlayerPrivileges(EditPlayerPrivilegesViewModel model)
        {
            var game = await GetOwnedGameAsync(model.GameId);
            if (game == null)
            {
                return NotFound();
            }

            if (model.UserId == game.CreatedByUserId)
            {
                TempData["GameMessage"] = "You cannot change privileges for the game owner.";
                return RedirectToAction(nameof(Players), new { id = game.Id });
            }

            var role = await _context.UserGameRoles.FirstOrDefaultAsync(rp => rp.GameId == game.Id && rp.UserId == model.UserId);
            if (role == null)
            {
                return NotFound();
            }

            var newPrivileges = GamePrivilege.None;
            if (model.SelectedPrivileges != null)
            {
                foreach (var privilege in model.SelectedPrivileges)
                {
                    newPrivileges |= privilege;
                }
            }

            role.Privileges = newPrivileges;
            await _context.SaveChangesAsync();

            TempData["GameMessage"] = $"Updated privileges for {model.UserName}.";
            return RedirectToAction(nameof(Players), new { id = game.Id });
        }

        public async Task<IActionResult> JoinCode(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await GetOwnedGameAsync(id.Value);
            if (game == null)
            {
                return NotFound();
            }

            var viewModel = new JoinCodeViewModel
            {
                GameId = game.Id,
                GameName = game.Name,
                JoinCode = game.JoinCode,
                JoinCodeActive = game.JoinCodeActive
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateJoinCode(int gameId)
        {
            var game = await GetOwnedGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            game.JoinCode = await GenerateUniqueJoinCodeAsync();
            game.JoinCodeActive = true;
            await _context.SaveChangesAsync();

            TempData["GameMessage"] = "Join code generated and activated.";
            return RedirectToAction(nameof(JoinCode), new { id = game.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJoinCode(int gameId, bool activate)
        {
            var game = await GetOwnedGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(game.JoinCode))
            {
                TempData["GameMessage"] = "Generate a join code first.";
                return RedirectToAction(nameof(JoinCode), new { id = game.Id });
            }

            game.JoinCodeActive = activate;
            await _context.SaveChangesAsync();

            TempData["GameMessage"] = activate ? "Join code activated." : "Join code deactivated.";
            return RedirectToAction(nameof(JoinCode), new { id = game.Id });
        }

        public IActionResult Join()
        {
            return View(new JoinGameViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(JoinGameViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var code = model.JoinCode?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError(nameof(model.JoinCode), "Enter a join code.");
                return View(model);
            }

            var game = await _context.Games
                .Include(g => g.UserGameRoles)
                .FirstOrDefaultAsync(g => g.JoinCode == code);

            if (game == null || string.IsNullOrWhiteSpace(game.JoinCode) || !game.JoinCodeActive)
            {
                ModelState.AddModelError(nameof(model.JoinCode), "Join code is invalid or deactivated.");
                return View(model);
            }

            var userId = GetCurrentUserId();
            var alreadyMember = game.CreatedByUserId == userId ||
                                game.UserGameRoles.Any(rp => rp.UserId == userId);
            if (alreadyMember)
            {
                ModelState.AddModelError(nameof(model.JoinCode), "You are already part of this game.");
                return View(model);
            }

            var rolePerson = new UserGameRole
            {
                GameId = game.Id,
                UserId = userId,
                IsOwner = false,
                Privileges = PrivilegeSets.Player
            };

            _context.UserGameRoles.Add(rolePerson);
            await _context.SaveChangesAsync();
            var actorName = await GetCurrentUserNameAsync();
            await LogAsync(game.Id, "UserAdded", $"{actorName} joined the game using a code", null, null, null);

            TempData["GameMessage"] = $"You joined \"{game.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        private IQueryable<Game> QueryUserGames(int userId)
        {
            return _context.Games.Where(g =>
                g.CreatedByUserId == userId ||
                g.UserGameRoles.Any(rp => rp.UserId == userId));
        }

        private Task<Game> GetOwnedGameAsync(int id)
        {
            var userId = GetCurrentUserId();
            return _context.Games.FirstOrDefaultAsync(g =>
                g.Id == id &&
                (g.CreatedByUserId == userId ||
                 g.UserGameRoles.Any(r => r.UserId == userId && r.IsOwner)));
        }

        private int GetCurrentUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue))
            {
                throw new System.InvalidOperationException("User identifier claim is missing.");
            }

            return int.Parse(claimValue);
        }

        private async Task<string> GetCurrentUserNameAsync()
        {
            var userId = GetCurrentUserId();
            var name = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(name) ? "Unknown user" : name;
        }

        private Task LogAsync(int gameId, string action, string details, int? characterId = null, int? itemId = null, int? categoryId = null)
        {
            return _historyLog.LogAsync(gameId, GetCurrentUserId(), action, details, characterId, itemId, categoryId);
        }

        private async Task<bool> IsOwnerAsync(int gameId)
        {
            var userId = GetCurrentUserId();
            return await _context.Games.AnyAsync(g => g.Id == gameId && g.CreatedByUserId == userId)
                || await _context.UserGameRoles.AnyAsync(r => r.GameId == gameId && r.UserId == userId && r.IsOwner);
        }

        private async Task<GamePrivilege> GetUserPrivilegesAsync(int gameId, bool? isOwner = null)
        {
            var ownerFlag = isOwner ?? await IsOwnerAsync(gameId);
            if (ownerFlag)
            {
                return GamePrivilege.All;
            }

            var userId = GetCurrentUserId();
            var privileges = await _context.UserGameRoles
                .Where(r => r.GameId == gameId && r.UserId == userId)
                .Select(r => r.Privileges)
                .FirstOrDefaultAsync();

            return privileges;
        }

        private async Task SetHistorySidebarAsync(int gameId, bool? isOwner = null)
        {
            var ownerFlag = isOwner ?? await IsOwnerAsync(gameId);
            ViewBag.HistorySidebar = new HistorySidebarViewModel
            {
                GameId = gameId,
                Logs = await _historyLog.GetRecentAsync(gameId, GetCurrentUserId(), ownerFlag)
            };
        }

        private async Task<string> GenerateUniqueJoinCodeAsync()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            string BuildSegment(int length)
            {
                var chars = new char[length];
                for (var i = 0; i < length; i++)
                {
                    chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
                }

                return new string(chars);
            }

            string code;
            do
            {
                code = $"{BuildSegment(3)}-{BuildSegment(4)}-{BuildSegment(4)}";
            }
            while (await _context.Games.AnyAsync(g => g.JoinCode == code));

            return code;
        }
    }
}
