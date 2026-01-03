using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DnDInventorySystem;
using DnDInventorySystem.Data;
using DnDInventorySystem.Models;

namespace DnDInventorySystem.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HistoryLogService _historyLog;
        private readonly IWebHostEnvironment _environment;

        public ItemsController(ApplicationDbContext context, HistoryLogService historyLog, IWebHostEnvironment environment)
        {
            _context = context;
            _historyLog = historyLog;
            _environment = environment;
        }
        //Returns all the items
        // GET: Items
        public async Task<IActionResult> Index(int? gameId, int page = 1)
        {
            const int PageSize = 10;
            if (page < 1) page = 1;
            var userId = GetCurrentUserId();
            var itemsQuery = _context.Items
                .Include(i => i.Category)
                .Include(i => i.CreatedByUser)
                .Include(i => i.Game)
                .AsQueryable();

            if (gameId.HasValue)
            {
                var privileges = await GetUserPrivilegesAsync(gameId.Value);
                if (!privileges.HasFlag(GamePrivilege.ViewItems))
                {
                    return NotFound();
                }
                itemsQuery = itemsQuery.Where(i => i.GameId == gameId.Value);
                var isOwner = await IsOwnerAsync(gameId.Value);
                if (!isOwner)
                {
                    itemsQuery = itemsQuery.Where(i =>
                        i.ViewableToPlayers || i.CreatedByUserId == userId);
                }
                ViewData["CurrentGameId"] = gameId.Value;
                ViewData["CurrentGameName"] = await _context.Games
                    .Where(g => g.Id == gameId.Value)
                    .Select(g => g.Name)
                    .FirstOrDefaultAsync();
                await SetHistorySidebarAsync(gameId.Value, isOwner);
                ViewBag.IsOwner = isOwner;
                ViewBag.Privileges = privileges;
            }
            ViewBag.CurrentUserId = userId;

            var totalCount = await itemsQuery.CountAsync();
            var items = await itemsQuery
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            return View(items);
        }
        //Returns a specific item details
        // GET: Items/Details/id
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.CreatedByUser)
                .Include(i => i.Game)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(item.GameId);
            var privileges = await GetUserPrivilegesAsync(item.GameId, isOwner);
            if (!privileges.HasFlag(GamePrivilege.ViewItems) && !isOwner)
            {
                return NotFound();
            }
            var userId = GetCurrentUserId();
            if (!isOwner && !item.ViewableToPlayers && item.CreatedByUserId != userId)
            {
                return NotFound();
            }

            ViewBag.CanEditItem = isOwner || item.CreatedByUserId == userId;
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(item.GameId, isOwner);
            return View(item);
        }
        //Item creation
        // GET: Items/Create
        public async Task<IActionResult> Create(int? gameId, int? returnCharacterId = null)
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
            if (!isOwner && !privileges.HasFlag(GamePrivilege.CreateItems))
            {
                return Forbid();
            }
            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await PopulateItemCreateViewAsync(game, returnCharacterId);
            await SetHistorySidebarAsync(game.Id, isOwner);
            return View();
        }

        // POST: Items/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int gameId, [Bind("Name,Description,CategoryId,ViewableToPlayers")] Item item, IFormFile? photoFile, int? returnCharacterId = null)
        {
            var game = await GetAuthorizedGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(game.Id);
            var privileges = await GetUserPrivilegesAsync(game.Id, isOwner);
            if (!isOwner && !privileges.HasFlag(GamePrivilege.CreateItems))
            {
                return Forbid();
            }

            if (item.CategoryId.HasValue)
            {
                var categoryBelongsToGame = await _context.Categories
                    .AnyAsync(c => c.Id == item.CategoryId && c.GameId == game.Id);
                if (!categoryBelongsToGame)
                {
                    ModelState.AddModelError(nameof(item.CategoryId), "Select a category from this game.");
                }
            }

            item.GameId = game.Id;
            item.CreatedByUserId = GetCurrentUserId();
            item.PhotoUrl = item.PhotoUrl ?? string.Empty;

            if (!isOwner)
            {
                item.ViewableToPlayers = true;
            }

            ValidateItemFields(item);
            if (ModelState.IsValid)
            {
                var uploadedPath = await SaveImageAsync(photoFile);
                if (!string.IsNullOrWhiteSpace(uploadedPath))
                {
                    item.PhotoUrl = uploadedPath;
                }

                _context.Add(item);
                await _context.SaveChangesAsync();
                var actor = await GetCurrentUserNameAsync();
                await LogAsync(game.Id, "ItemCreated", $"Item {item.Name} created by {actor}", itemId: item.Id);
                if (returnCharacterId.HasValue)
                {
                    return RedirectToAction("AssignItems", "Characters", new { id = returnCharacterId.Value });
                }

                return RedirectToAction(nameof(Index), new { gameId = game.Id });
            }
            await PopulateItemCreateViewAsync(game, returnCharacterId);
            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(game.Id, isOwner);
            return View(item);
        }
        //Editing item
        // GET: Items/Edit/id
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Game)
                .Include(i => i.CreatedByUser)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(item.GameId);
            var privileges = await GetUserPrivilegesAsync(item.GameId, isOwner);
            if (!isOwner && (!privileges.HasFlag(GamePrivilege.EditItems) || item.CreatedByUserId != GetCurrentUserId()))
            {
                return Forbid();
            }
            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await PopulateItemEditViewAsync(item);
            await SetHistorySidebarAsync(item.GameId, isOwner);
            return View(item);
        }

        // POST: Items/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CategoryId,ViewableToPlayers")] Item formItem, IFormFile? photoFile)
        {
            if (id != formItem.Id)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Game)
                .Include(i => i.CreatedByUser)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(item.GameId);
            var privileges = await GetUserPrivilegesAsync(item.GameId, isOwner);
            if (!isOwner && (!privileges.HasFlag(GamePrivilege.EditItems) || item.CreatedByUserId != GetCurrentUserId()))
            {
                return Forbid();
            }

            if (formItem.CategoryId.HasValue)
            {
                var categoryBelongsToGame = await _context.Categories
                    .AnyAsync(c => c.Id == formItem.CategoryId && c.GameId == item.GameId);
                if (!categoryBelongsToGame)
                {
                    ModelState.AddModelError(nameof(item.CategoryId), "Select a category from this game.");
                }
            }

            ValidateItemFields(formItem);
            if (ModelState.IsValid)
            {
                var actor = await GetCurrentUserNameAsync();
                var oldName = item.Name;
                item.Name = formItem.Name;
                item.Description = formItem.Description;
                if (isOwner)
                {
                    item.ViewableToPlayers = formItem.ViewableToPlayers;
                }
                var uploadedPath = await SaveImageAsync(photoFile);
                if (!string.IsNullOrWhiteSpace(uploadedPath))
                {
                    item.PhotoUrl = uploadedPath;
                }
                item.CategoryId = formItem.CategoryId;
                await _context.SaveChangesAsync();
                await LogAsync(item.GameId, "ItemEdited", $"Item {item.Name} edited by {actor}", itemId: item.Id);
                return RedirectToAction(nameof(Index), new { gameId = item.GameId });
            }

            await PopulateItemEditViewAsync(item);
            item.Name = formItem.Name;
            item.Description = formItem.Description;
            item.CategoryId = formItem.CategoryId;
            item.ViewableToPlayers = item.ViewableToPlayers;
            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(item.GameId, isOwner);
            return View(item);
        }
        //Item deletion
        // GET: Items/Delete/id
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.CreatedByUser)
                .Include(i => i.Game)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(item.GameId);
            var userId = GetCurrentUserId();
            if (!isOwner && item.CreatedByUserId != userId)
            {
                return Forbid();
            }

            ViewBag.IsOwner = isOwner;
            await SetHistorySidebarAsync(item.GameId, isOwner);
            return View(item);
        }

        // POST: Items/Delete/id
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                var isOwner = await IsOwnerAsync(item.GameId);
                var privileges = await GetUserPrivilegesAsync(item.GameId, isOwner);
                var userId = GetCurrentUserId();
                if (!isOwner && (!privileges.HasFlag(GamePrivilege.DeleteItems) || item.CreatedByUserId != userId))
                {
                    return Forbid();
                }

                var actor = await GetCurrentUserNameAsync();
                await LogAsync(item.GameId, "ItemDeleted", $"Item {item.Name} deleted by {actor}", itemId: item.Id);
                var itemAssignments = await _context.ItemCharacters
                    .Where(ic => ic.ItemId == item.Id)
                    .ToListAsync();
                if (itemAssignments.Any())
                {
                    _context.ItemCharacters.RemoveRange(itemAssignments);
                }
                var itemLogs = await _context.HistoryLogs
                    .Where(h => h.ItemId == item.Id)
                    .ToListAsync();
                if (itemLogs.Any())
                {
                    _context.HistoryLogs.RemoveRange(itemLogs);
                }
                _context.Items.Remove(item);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { gameId = item?.GameId });
        }


        private Task<Game> GetAuthorizedGameAsync(int gameId)
        {
            var userId = GetCurrentUserId();
            return _context.Games
                .FirstOrDefaultAsync(g => g.Id == gameId &&
                    (g.CreatedByUserId == userId ||
                    g.UserGameRoles.Any(rp => rp.UserId == userId)));
        }

        private async Task<bool> IsOwnerAsync(int gameId)
        {
            var userId = GetCurrentUserId();
            return await _context.Games.AnyAsync(g => g.Id == gameId && g.CreatedByUserId == userId)
                || await _context.UserGameRoles.AnyAsync(r => r.GameId == gameId && r.UserId == userId && r.IsOwner);
        }

        private async Task PopulateItemCreateViewAsync(Game game, int? returnCharacterId = null)
        {
            var categories = await _context.Categories
                .Where(c => c.GameId == game.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name");
            ViewData["CurrentGameId"] = game.Id;
            ViewData["CurrentGameName"] = game.Name;
            if (returnCharacterId.HasValue)
            {
                ViewData["ReturnCharacterId"] = returnCharacterId.Value;
            }
        }

        private async Task PopulateItemEditViewAsync(Item item)
        {
            var categories = await _context.Categories
                .Where(c => c.GameId == item.GameId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", item.CategoryId);
            ViewData["GameName"] = item.Game?.Name ?? "Unknown game";
            ViewData["CreatorName"] = item.CreatedByUser?.Name ?? "Unknown user";
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

        private void ValidateItemFields(Item item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                ModelState.AddModelError(nameof(item.Name), "Please fill in the Inventory name!");
            }
            if (!string.IsNullOrWhiteSpace(item.Name) && (item.Name.Length < 1 || item.Name.Length > 200))
            {
                ModelState.AddModelError(nameof(item.Name), "Character limit exceeded for the name!");
            }
            if (!string.IsNullOrWhiteSpace(item.Description) && item.Description.Length > 2000)
            {
                ModelState.AddModelError(nameof(item.Description), "Character limit exceeded for the description!");
            }
            if (!string.IsNullOrWhiteSpace(item.PhotoUrl) && item.PhotoUrl.Length > 2000)
            {
                ModelState.AddModelError(nameof(item.PhotoUrl), "Character limit exceeded for the image!");
            }
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

        private async Task SetHistorySidebarAsync(int gameId, bool? isOwner = null)
        {
            var ownerFlag = isOwner ?? await IsOwnerAsync(gameId);
            ViewBag.HistorySidebar = new ViewModels.HistorySidebarViewModel
            {
                GameId = gameId,
                Logs = await _historyLog.GetRecentAsync(gameId, GetCurrentUserId(), ownerFlag)
            };
        }

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var uploadsRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")
                : Path.Combine(_environment.WebRootPath, "uploads");

            Directory.CreateDirectory(uploadsRoot);

            var extension = Path.GetExtension(file.FileName);
            var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
            var fileName = $"{Guid.NewGuid():N}{safeExtension}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{fileName}";
        }
    }
}
