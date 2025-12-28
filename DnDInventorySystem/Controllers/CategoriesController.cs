using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DnDInventorySystem;
using DnDInventorySystem.Data;
using DnDInventorySystem.Models;

namespace DnDInventorySystem.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HistoryLogService _historyLog;

        public CategoriesController(ApplicationDbContext context, HistoryLogService historyLog)
        {
            _context = context;
            _historyLog = historyLog;
        }

        // GET: Categories
        public async Task<IActionResult> Index(int? gameId, int page = 1)
        {
            const int PageSize = 10;
            if (page < 1) page = 1;
            if (!gameId.HasValue)
            {
                return NotFound();
            }

            var game = await GetAuthorizedGameAsync(gameId.Value);
            if (game == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(game.Id);
            var privileges = await GetUserPrivilegesAsync(game.Id, isOwner);
            if (!privileges.HasFlag(GamePrivilege.ViewCategories) && !isOwner)
            {
                return NotFound();
            }

            var categories = await _context.Categories
                .Include(c => c.CreatedByUser)
                .Include(c => c.Game)
                .Include(c => c.Items)
                .Where(c => c.GameId == game.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var currentUserId = GetCurrentUserId();
            if (!isOwner)
            {
                foreach (var category in categories)
                {
                    category.Items = category.Items
                        .Where(i => i.ViewableToPlayers || i.CreatedByUserId == currentUserId)
                        .ToList();
                }
            }

            ViewData["CurrentGameId"] = game.Id;
            ViewData["CurrentGameName"] = game.Name;
            ViewBag.IsOwner = isOwner;
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.Privileges = privileges;
            var totalCount = categories.Count;
            var pageCategories = categories
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            await SetHistorySidebarAsync(game.Id, isOwner);
            return View(pageCategories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.CreatedByUser)
                .Include(c => c.Game)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(category.GameId);
            var privileges = await GetUserPrivilegesAsync(category.GameId, isOwner);
            if (!isOwner && !privileges.HasFlag(GamePrivilege.ViewCategories))
            {
                return NotFound();
            }
            ViewBag.CanEditCategory = isOwner || category.CreatedByUserId == GetCurrentUserId();
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(category.GameId, isOwner);
            return View(category);
        }

        // GET: Categories/Create
        public async Task<IActionResult> Create(int? gameId)
        {
            if (gameId == null)
            {
                return NotFound();
            }

            var game = await GetAuthorizedGameAsync(gameId.Value);
            if (game == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(game.Id);
            var privileges = await GetUserPrivilegesAsync(game.Id, isOwner);
            if (!isOwner && !privileges.HasFlag(GamePrivilege.CreateCategories))
            {
                return Forbid();
            }

            PopulateCategoryCreateView(game);
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(game.Id, isOwner);
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int gameId, [Bind("Name")] Category category)
        {
            var game = await GetAuthorizedGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(game.Id);
            var privileges = await GetUserPrivilegesAsync(game.Id, isOwner);
            if (!isOwner && !privileges.HasFlag(GamePrivilege.CreateCategories))
            {
                return Forbid();
            }

            ValidateCategoryFields(category);
            category.GameId = game.Id;
            category.CreatedByUserId = GetCurrentUserId();

            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                var actor = await GetCurrentUserNameAsync();
                await LogAsync(game.Id, "CategoryCreated", $"Category {category.Name} created by {actor}", categoryId: category.Id);
                return RedirectToAction(nameof(Index), new { gameId = game.Id });
            }

            PopulateCategoryCreateView(game);
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(game.Id, isOwner);
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(category.GameId);
            var privileges = await GetUserPrivilegesAsync(category.GameId, isOwner);
            if (!isOwner && !privileges.HasFlag(GamePrivilege.EditCategories))
            {
                return Forbid();
            }

            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(category.GameId, isOwner);
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,GameId,CreatedByUserId")] Category formCategory)
        {
            if (id != formCategory.Id)
            {
                return NotFound();
            }

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(category.GameId);
            var privileges = await GetUserPrivilegesAsync(category.GameId, isOwner);
            if (!isOwner && !privileges.HasFlag(GamePrivilege.EditCategories))
            {
                return Forbid();
            }

            ValidateCategoryFields(formCategory);
            if (ModelState.IsValid)
            {
                try
                {
                    var oldName = category.Name;
                    category.Name = formCategory.Name;
                    await _context.SaveChangesAsync();

                    var actor = await GetCurrentUserNameAsync();
                    if (!string.Equals(oldName, category.Name, StringComparison.Ordinal))
                    {
                        await LogAsync(category.GameId, "CategoryRenamed", $"Category {oldName} changed the name to {category.Name} by {actor}", categoryId: category.Id);
                    }
                    else
                    {
                        await LogAsync(category.GameId, "CategoryEdited", $"Category {category.Name} edited by {actor}", categoryId: category.Id);
                    }

                    return RedirectToAction(nameof(Index), new { gameId = category.GameId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(formCategory.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            await SetHistorySidebarAsync(category.GameId, isOwner);
            return View(formCategory);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.CreatedByUser)
                .Include(c => c.Game)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(category.GameId);
            var privileges = await GetUserPrivilegesAsync(category.GameId, isOwner);
            if (!isOwner && !privileges.HasFlag(GamePrivilege.DeleteCategories))
            {
                return Forbid();
            }

            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(category.GameId, isOwner);
            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category != null)
            {
                var isOwner = await IsOwnerAsync(category.GameId);
                var privileges = await GetUserPrivilegesAsync(category.GameId, isOwner);
                if (!isOwner && !privileges.HasFlag(GamePrivilege.DeleteCategories))
                {
                    return Forbid();
                }

                var linkedToItems = await _context.Items.AnyAsync(i => i.CategoryId == category.Id);
                if (linkedToItems)
                {
                    TempData["CategoryMessage"] = "The category is already linked to an inventory, first remove the link from the inventory!";
                    return RedirectToAction(nameof(Index), new { gameId = category.GameId });
                }

                var actor = await GetCurrentUserNameAsync();
                await LogAsync(category.GameId, "CategoryDeleted", $"Category {category.Name} deleted by {actor}", categoryId: category.Id);
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { gameId = category?.GameId });
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
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

        private async Task SetHistorySidebarAsync(int gameId, bool? isOwner = null)
        {
            var ownerFlag = isOwner ?? await IsOwnerAsync(gameId);
            ViewBag.HistorySidebar = new ViewModels.HistorySidebarViewModel
            {
                GameId = gameId,
                Logs = await _historyLog.GetRecentAsync(gameId, GetCurrentUserId(), ownerFlag)
            };
        }

        private Task<Game> GetAuthorizedGameAsync(int gameId)
        {
            var userId = GetCurrentUserId();
            return _context.Games
                .FirstOrDefaultAsync(g => g.Id == gameId &&
                    (g.CreatedByUserId == userId ||
                     g.UserGameRoles.Any(rp => rp.UserId == userId)));
        }

        private void PopulateCategoryCreateView(Game game)
        {
            ViewData["CurrentGameId"] = game.Id;
            ViewData["CurrentGameName"] = game.Name;
        }

        private async Task<GamePrivilege> GetUserPrivilegesAsync(int gameId, bool? isOwner = null)
        {
            var ownerFlag = isOwner ?? await IsOwnerAsync(gameId);
            if (ownerFlag)
            {
                return GamePrivilege.All;
            }

            var userId = GetCurrentUserId();
            return await _context.UserGameRoles
                .Where(r => r.GameId == gameId && r.UserId == userId)
                .Select(r => r.Privileges)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> IsOwnerAsync(int gameId)
        {
            var userId = GetCurrentUserId();
            return await _context.Games.AnyAsync(g => g.Id == gameId && g.CreatedByUserId == userId)
                || await _context.UserGameRoles.AnyAsync(r => r.GameId == gameId && r.UserId == userId && r.IsOwner);
        }

        private void ValidateCategoryFields(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError(nameof(category.Name), "Please fill in the category name!");
            }
            if (!string.IsNullOrWhiteSpace(category.Name) && (category.Name.Length < 1 || category.Name.Length > 200))
            {
                ModelState.AddModelError(nameof(category.Name), "Character limit exceeded for the name!");
            }
        }
    }
}
