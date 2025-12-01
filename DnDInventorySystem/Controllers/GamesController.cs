using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DnDInventorySystem.Data;
using DnDInventorySystem.Models;
using DnDInventorySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DnDInventorySystem.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        private const string PlayerRoleName = "Player";
        private readonly ApplicationDbContext _context;

        public GamesController(ApplicationDbContext context)
        {
            _context = context;
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

            var memberRole = await EnsureRoleAsync(PlayerRoleName);

            game.RolePersons.Add(new RolePerson
            {
                UserId = userId,
                RoleId = memberRole.Id
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

        public async Task<IActionResult> Join()
        {
            var userId = GetCurrentUserId();

            var availableGames = await _context.Games
                .Include(g => g.CreatedByUser)
                .Where(g => g.CreatedByUserId != userId &&
                            !g.RolePersons.Any(rp => rp.UserId == userId))
                .OrderBy(g => g.Name)
                .ToListAsync();

            return View(availableGames);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinGame(int id)
        {
            var userId = GetCurrentUserId();
            var game = await _context.Games
                .Include(g => g.RolePersons)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            var alreadyMember = game.CreatedByUserId == userId ||
                                game.RolePersons.Any(rp => rp.UserId == userId);
            if (alreadyMember)
            {
                return RedirectToAction(nameof(Index));
            }

            var memberRole = await EnsureRoleAsync(PlayerRoleName);

            var rolePerson = new RolePerson
            {
                GameId = game.Id,
                UserId = userId,
                RoleId = memberRole.Id
            };

            _context.RolePersons.Add(rolePerson);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private IQueryable<Game> QueryUserGames(int userId)
        {
            return _context.Games.Where(g =>
                g.CreatedByUserId == userId ||
                g.RolePersons.Any(rp => rp.UserId == userId));
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

        private async Task<Role> EnsureRoleAsync(string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role != null)
            {
                return role;
            }

            role = new Role { Name = roleName };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }
    }
}
