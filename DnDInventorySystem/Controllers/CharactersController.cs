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
using DnDInventorySystem.ViewModels;

namespace DnDInventorySystem.Controllers
{
    [Authorize]
    public class CharactersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HistoryLogService _historyLog;
        private readonly IWebHostEnvironment _environment;

        public CharactersController(ApplicationDbContext context, HistoryLogService historyLog, IWebHostEnvironment environment)
        {
            _context = context;
            _historyLog = historyLog;
            _environment = environment;
        }

        // GET: Characters
        public async Task<IActionResult> Index(int? gameId, int page = 1)
        {
            const int PageSize = 10;
            if (page < 1) page = 1;
            var charactersQuery = _context.Characters
                .Include(c => c.CreatedByUser)
                .Include(c => c.Game)
                .Include(c => c.Owner)
                .AsQueryable();

            if (gameId.HasValue)
            {
                var privileges = await GetUserPrivilegesAsync(gameId.Value);
                if (!privileges.HasFlag(GamePrivilege.ViewCharacters))
                {
                    return NotFound();
                }
                charactersQuery = charactersQuery.Where(c => c.GameId == gameId.Value);
                var isOwner = await IsOwnerAsync(gameId.Value);
                var userId = GetCurrentUserId();
                if (!isOwner)
                {
                    charactersQuery = charactersQuery.Where(c =>
                        c.ViewableToPlayers ||
                        c.OwnerUserId == userId ||
                        c.CreatedByUserId == userId);
                }
                ViewData["CurrentGameId"] = gameId.Value;
                ViewData["CurrentGameName"] = await _context.Games
                    .Where(g => g.Id == gameId.Value)
                    .Select(g => g.Name)
                    .FirstOrDefaultAsync();
                ViewBag.IsOwner = isOwner;
                ViewBag.CurrentUserId = userId;
                ViewBag.Privileges = privileges;
            }

            if (ViewBag.CurrentUserId == null)
            {
                ViewBag.CurrentUserId = GetCurrentUserId();
            }
            var totalCount = await charactersQuery.CountAsync();
            var characters = await charactersQuery
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            return View(characters);
        }

        // GET: Characters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var character = await GetAuthorizedCharacterAsync(id.Value);
            if (character == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(character.GameId);
            var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
            if (!privileges.HasFlag(GamePrivilege.ViewCharacters) && !isOwner)
            {
                return NotFound();
            }
            var userId = GetCurrentUserId();
            if (!isOwner && !character.ViewableToPlayers && character.OwnerUserId != userId && character.CreatedByUserId != userId)
            {
                return NotFound();
            }

            ViewBag.CanEditCharacter = isOwner || character.CreatedByUserId == userId || character.OwnerUserId == userId;
            ViewBag.CanAssignItems = isOwner ||
                (privileges.HasFlag(GamePrivilege.AddItemsToCharacters) &&
                 (character.OwnerUserId == userId || character.CreatedByUserId == userId));
            var viewModel = await BuildCharacterInventoryViewModel(character);
            ViewBag.Privileges = privileges;
            ViewBag.IsOwner = isOwner;
            await SetHistorySidebarAsync(character.GameId, isOwner);
            return View(viewModel);
        }

        // GET: Characters/Create
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
            if (!isOwner && !privileges.HasFlag(GamePrivilege.CreateCharacters))
            {
                return Forbid();
            }
            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await PopulateCharacterCreateViewAsync(game);
            await SetHistorySidebarAsync(game.Id, isOwner);
            return View();
        }

        // POST: Characters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int gameId, [Bind("Name,Description,OwnerUserId,ViewableToPlayers")] Character character, IFormFile? photoFile)
        {
            var game = await GetAuthorizedGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(game.Id);
            var privileges = await GetUserPrivilegesAsync(game.Id, isOwner);
            if (!isOwner && !privileges.HasFlag(GamePrivilege.CreateCharacters))
            {
                return Forbid();
            }

            var validOwners = await GetGameUsersAsync(game);
            if (!validOwners.Any(u => u.Id == character.OwnerUserId))
            {
                ModelState.AddModelError(nameof(character.OwnerUserId), "Select a user who is part of this game.");
            }

            character.GameId = game.Id;
            character.CreatedByUserId = GetCurrentUserId();
            character.PhotoUrl = string.Empty;
            if (!isOwner)
            {
                character.ViewableToPlayers = true;
            }

            if (ModelState.IsValid)
            {
                var uploadedPath = await SaveImageAsync(photoFile);
                if (!string.IsNullOrWhiteSpace(uploadedPath))
                {
                    character.PhotoUrl = uploadedPath;
                }

                _context.Add(character);
                await _context.SaveChangesAsync();
                var actorName = await GetCurrentUserNameAsync();
                await LogAsync(game.Id, "CharacterCreated", $"Character {character.Name} created by {actorName}", characterId: character.Id);
                if (character.OwnerUserId > 0)
                {
                    var ownerName = validOwners.FirstOrDefault(o => o.Id == character.OwnerUserId)?.Name ?? "User";
                    await LogAsync(game.Id, "CharacterAssignedOwner", $"Character {character.Name} assigned to {ownerName} by {actorName}", characterId: character.Id);
                }
                return RedirectToAction("Details", "Games", new { id = game.Id });
            }

            await PopulateCharacterCreateViewAsync(game, character.OwnerUserId, validOwners);
            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(game.Id, isOwner);
            return View(character);
        }

        // GET: Characters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var character = await GetAuthorizedCharacterAsync(id.Value);
            if (character == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(character.GameId);
            var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
            var userId = GetCurrentUserId();
            if (!isOwner)
            {
                var isAssignedOwner = character.OwnerUserId == userId;
                var isCreator = character.CreatedByUserId == userId;

                if (!isAssignedOwner && !isCreator)
                {
                    return Forbid();
                }

                if (!privileges.HasFlag(GamePrivilege.EditCharacters) && !isAssignedOwner)
                {
                    return Forbid();
                }
            }

            await PopulateCharacterEditViewAsync(character, character.OwnerUserId);
            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(character.GameId, isOwner);
            return View(character);
        }

        // POST: Characters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,OwnerUserId,ViewableToPlayers")] Character formCharacter, IFormFile? photoFile)
        {
            if (id != formCharacter.Id)
            {
                return NotFound();
            }

            var character = await GetAuthorizedCharacterAsync(id);
            if (character == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(character.GameId);
            var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
            var userId = GetCurrentUserId();
            if (!isOwner)
            {
                var isAssignedOwner = character.OwnerUserId == userId;
                var isCreator = character.CreatedByUserId == userId;

                if (!isAssignedOwner && !isCreator)
                {
                    return Forbid();
                }

                if (!privileges.HasFlag(GamePrivilege.EditCharacters) && !isAssignedOwner)
                {
                    return Forbid();
                }
            }

            var validOwners = await GetGameUsersAsync(character.Game);
            if (!validOwners.Any(u => u.Id == formCharacter.OwnerUserId))
            {
                ModelState.AddModelError(nameof(formCharacter.OwnerUserId), "Select a user who is part of this game.");
            }

            if (ModelState.IsValid)
            {
                var actorName = await GetCurrentUserNameAsync();
                var previousOwnerId = character.OwnerUserId;

                character.Name = formCharacter.Name;
                character.Description = formCharacter.Description;
                character.OwnerUserId = formCharacter.OwnerUserId;
                if (isOwner)
                {
                    character.ViewableToPlayers = formCharacter.ViewableToPlayers;
                }
                var uploadedPath = await SaveImageAsync(photoFile);
                if (!string.IsNullOrWhiteSpace(uploadedPath))
                {
                    character.PhotoUrl = uploadedPath;
                }
                await _context.SaveChangesAsync();
                await LogAsync(character.GameId, "CharacterEdited", $"Character {character.Name} edited by {actorName}", characterId: character.Id);
                if (previousOwnerId != formCharacter.OwnerUserId)
                {
                    var newOwner = validOwners.FirstOrDefault(o => o.Id == formCharacter.OwnerUserId)?.Name ?? "User";
                    await LogAsync(character.GameId, "CharacterAssignedOwner", $"Character {character.Name} assigned to {newOwner} by {actorName}", characterId: character.Id);
                }
                return RedirectToAction(nameof(Index), new { gameId = character.GameId });
            }

            await PopulateCharacterEditViewAsync(character, formCharacter.OwnerUserId, validOwners);
            character.Name = formCharacter.Name;
            character.Description = formCharacter.Description;
            character.OwnerUserId = formCharacter.OwnerUserId;
            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(character.GameId, isOwner);
            return View(character);
        }

        // GET: Characters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var character = await _context.Characters
                .Include(c => c.CreatedByUser)
                .Include(c => c.Game)
                .Include(c => c.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (character == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(character.GameId);
            var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
            if (!isOwner && (!privileges.HasFlag(GamePrivilege.DeleteCharacters) || character.CreatedByUserId != GetCurrentUserId()))
            {
                return Forbid();
            }

            ViewBag.IsOwner = isOwner;
            ViewBag.Privileges = privileges;
            await SetHistorySidebarAsync(character.GameId, isOwner);
            return View(character);
        }

        // POST: Characters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var character = await _context.Characters.FirstOrDefaultAsync(c => c.Id == id);
            if (character != null)
            {
                var isOwner = await IsOwnerAsync(character.GameId);
                var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
                if (!isOwner && (!privileges.HasFlag(GamePrivilege.DeleteCharacters) || character.CreatedByUserId != GetCurrentUserId()))
                {
                    return Forbid();
                }

                var assignments = await _context.ItemCharacters
                    .Where(ic => ic.CharacterId == character.Id)
                    .ToListAsync();
                if (assignments.Any())
                {
                    _context.ItemCharacters.RemoveRange(assignments);
                }

                var characterLogs = await _context.HistoryLogs
                    .Where(h => h.CharacterId == character.Id)
                    .ToListAsync();
                if (characterLogs.Any())
                {
                    _context.HistoryLogs.RemoveRange(characterLogs);
                }

                var actorName = await GetCurrentUserNameAsync();
                await LogAsync(character.GameId, "CharacterDeleted", $"Character {character.Name} deleted by {actorName}", characterId: character.Id);
                _context.Characters.Remove(character);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInventory(int characterId, int entryId, int quantity, bool isEquipped)
        {
            var character = await GetAuthorizedCharacterAsync(characterId);
            if (character == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(character.GameId);
            var userId = GetCurrentUserId();
            var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
            if (!isOwner && (!privileges.HasFlag(GamePrivilege.EditCharacterInventory) || (character.OwnerUserId != userId && character.CreatedByUserId != userId)))
            {
                return Forbid();
            }

            var entry = await _context.ItemCharacters
                .FirstOrDefaultAsync(ic => ic.Id == entryId && ic.CharacterId == character.Id);
            if (entry == null)
            {
                return NotFound();
            }

            entry.Quantity = quantity < 1 ? 1 : quantity;
            entry.IsEquipped = isEquipped;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = character.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInventoryBulk(int characterId, List<InventoryUpdateRow> updates)
        {
            var character = await GetAuthorizedCharacterAsync(characterId);
            if (character == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(character.GameId);
            var userId = GetCurrentUserId();
            var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
            if (!isOwner && (!privileges.HasFlag(GamePrivilege.EditCharacterInventory) || (character.OwnerUserId != userId && character.CreatedByUserId != userId)))
            {
                return Forbid();
            }

            if (updates == null || updates.Count == 0)
            {
                return RedirectToAction(nameof(Details), new { id = character.Id });
            }

            var entryIds = updates.Select(u => u.EntryId).ToList();
            var entries = await _context.ItemCharacters
                .Where(ic => ic.CharacterId == character.Id && entryIds.Contains(ic.Id))
                .Include(ic => ic.Item)
                .ToListAsync();
            var actorName = await GetCurrentUserNameAsync();

            foreach (var update in updates)
            {
                var entry = entries.FirstOrDefault(e => e.Id == update.EntryId);
                if (entry == null)
                {
                    continue;
                }

                entry.Quantity = update.Quantity < 1 ? 1 : update.Quantity;
                entry.IsEquipped = update.IsEquipped;
                if (entry.Item != null)
                {
                    await LogAsync(character.GameId, "ItemEdited", $"Character's {entry.Item.Name} edited by {actorName}", characterId: character.Id, itemId: entry.ItemId);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = character.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveInventory(int characterId, int entryId)
        {
            var character = await GetAuthorizedCharacterAsync(characterId);
            if (character == null)
            {
                return NotFound();
            }

            var entry = await _context.ItemCharacters
                .FirstOrDefaultAsync(ic => ic.Id == entryId && ic.CharacterId == character.Id);
            if (entry != null)
            {
                var isOwner = await IsOwnerAsync(character.GameId);
                var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
                var userId = GetCurrentUserId();
                if (!isOwner && (!privileges.HasFlag(GamePrivilege.RemoveItemsFromCharacters) || (character.OwnerUserId != userId && character.CreatedByUserId != userId)))
                {
                    return Forbid();
                }

                _context.ItemCharacters.Remove(entry);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = character.Id });
        }

        private bool CharacterExists(int id)
        {
            return _context.Characters.Any(e => e.Id == id);
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

        private Task<Game> GetAuthorizedGameAsync(int gameId)
        {
            var userId = GetCurrentUserId();
            return _context.Games
                .FirstOrDefaultAsync(g => g.Id == gameId &&
                    (g.CreatedByUserId == userId ||
                     g.UserGameRoles.Any(rp => rp.UserId == userId)));
        }

        private async Task<List<User>> GetGameUsersAsync(Game game)
        {
            var participantIds = await _context.UserGameRoles
                .Where(rp => rp.GameId == game.Id)
                .Select(rp => rp.UserId)
                .ToListAsync();

            if (!participantIds.Contains(game.CreatedByUserId))
            {
                participantIds.Add(game.CreatedByUserId);
            }

            return await _context.Users
                .Where(u => participantIds.Contains(u.Id))
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        private async Task PopulateCharacterCreateViewAsync(Game game, int? selectedOwnerId = null, IEnumerable<User>? owners = null)
        {
            var ownerList = owners ?? await GetGameUsersAsync(game);
            ViewData["OwnerUserId"] = new SelectList(ownerList, "Id", "Name", selectedOwnerId);
            ViewData["CurrentGameId"] = game.Id;
            ViewData["CurrentGameName"] = game.Name;
        }

        private async Task PopulateCharacterEditViewAsync(Character character, int? selectedOwnerId = null, IEnumerable<User>? owners = null)
        {
            var ownerList = owners ?? await GetGameUsersAsync(character.Game);
            ViewData["OwnerUserId"] = new SelectList(ownerList, "Id", "Name", selectedOwnerId ?? character.OwnerUserId);
        }

        // GET: Characters/AssignItems/5
        public async Task<IActionResult> AssignItems(int id, int page = 1)
        {
            const int PageSize = 10;
            if (page < 1) page = 1;
            var character = await GetAuthorizedCharacterAsync(id);
            if (character == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(character.GameId);
            var userId = GetCurrentUserId();
            var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
            if (!isOwner && (!privileges.HasFlag(GamePrivilege.AddItemsToCharacters) || (character.OwnerUserId != userId && character.CreatedByUserId != userId)))
            {
                return Forbid();
            }

            var viewModel = await BuildCharacterAssignItemsViewModel(character, page, PageSize);
            await SetHistorySidebarAsync(character.GameId, isOwner);
            return View(viewModel);
        }

        // POST: Characters/AssignItems
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignItems(CharacterAssignItemsViewModel model, int page = 1)
        {
            if (model.CharacterId == 0)
            {
                return NotFound();
            }

            var character = await GetAuthorizedCharacterAsync(model.CharacterId);
            if (character == null)
            {
                return NotFound();
            }

            var isOwner = await IsOwnerAsync(character.GameId);
            var userId = GetCurrentUserId();
            var privileges = await GetUserPrivilegesAsync(character.GameId, isOwner);
            if (!isOwner && (!privileges.HasFlag(GamePrivilege.AddItemsToCharacters) || (character.OwnerUserId != userId && character.CreatedByUserId != userId)))
            {
                return Forbid();
            }

            if (model.Assignments == null || !model.Assignments.Any(a => a.Selected))
            {
                ModelState.AddModelError(string.Empty, "Select at least one item to assign.");
            }

            if (!ModelState.IsValid)
            {
                var vm = await BuildCharacterAssignItemsViewModel(character, page, 10);
                await SetHistorySidebarAsync(character.GameId, isOwner);
                return View(vm);
            }

            var actorName = await GetCurrentUserNameAsync();
            var selectedIds = model.Assignments.Where(a => a.Selected).Select(a => a.ItemId).ToList();
            var itemNames = await _context.Items
                .Where(i => selectedIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Name);

            foreach (var row in model.Assignments.Where(a => a.Selected))
            {
                if (row.Quantity < 1)
                {
                    continue;
                }

                var itemValid = await _context.Items.AnyAsync(i => i.Id == row.ItemId && i.GameId == character.GameId);
                if (!itemValid)
                {
                    continue;
                }

                var existing = await _context.ItemCharacters
                    .FirstOrDefaultAsync(ic => ic.CharacterId == character.Id && ic.ItemId == row.ItemId);

                if (existing == null)
                {
                    _context.ItemCharacters.Add(new ItemCharacter
                    {
                        CharacterId = character.Id,
                        ItemId = row.ItemId,
                        Quantity = row.Quantity,
                        IsEquipped = row.IsEquipped
                    });
                    var itemName = itemNames.TryGetValue(row.ItemId, out var nm1) ? nm1 : "Item";
                    await LogAsync(character.GameId, "ItemAssigned", $"Character {character.Name} assigned {itemName} by {actorName}", characterId: character.Id, itemId: row.ItemId);
                }
                else
                {
                    var previousQuantity = existing.Quantity;
                    var previousEquipped = existing.IsEquipped;
                    existing.Quantity = row.Quantity;
                    existing.IsEquipped = row.IsEquipped;
                    if (previousQuantity != row.Quantity || previousEquipped != row.IsEquipped)
                    {
                        var itemName = itemNames.TryGetValue(row.ItemId, out var nm2) ? nm2 : "Item";
                        await LogAsync(character.GameId, "ItemEdited", $"Character's {itemName} edited by {actorName}", characterId: character.Id, itemId: row.ItemId);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = character.Id });
        }

        private async Task<Character> GetAuthorizedCharacterAsync(int characterId)
        {
            var character = await _context.Characters
                .Include(c => c.Game)
                .Include(c => c.CreatedByUser)
                .Include(c => c.Owner)
                .FirstOrDefaultAsync(c => c.Id == characterId);

            if (character == null)
            {
                return null;
            }

            var game = await GetAuthorizedGameAsync(character.GameId);
            return game == null ? null : character;
        }

        private async Task<CharacterInventoryViewModel> BuildCharacterInventoryViewModel(Character character)
        {
            var inventory = await _context.ItemCharacters
                .Where(ic => ic.CharacterId == character.Id)
                .Include(ic => ic.Item)
                .ThenInclude(i => i.Category)
                .OrderBy(ic => ic.Item.Name)
                .ToListAsync();

            return new CharacterInventoryViewModel
            {
                Character = character,
                Inventory = inventory,
                Updates = inventory.Select(ic => new InventoryUpdateRow
                {
                    EntryId = ic.Id,
                    Quantity = ic.Quantity,
                    IsEquipped = ic.IsEquipped
                }).ToList()
            };
        }

        private async Task<CharacterAssignItemsViewModel> BuildCharacterAssignItemsViewModel(Character character, int page = 1, int pageSize = 10)
        {
            var existingAssignments = await _context.ItemCharacters
                .Where(ic => ic.CharacterId == character.Id)
                .ToListAsync();

            var itemsQuery = _context.Items
                .Where(i => i.GameId == character.GameId)
                .OrderBy(i => i.Name)
                .Include(i => i.Category);

            var totalCount = await itemsQuery.CountAsync();
            if (page < 1) page = 1;
            var items = await itemsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var assignments = items.Select(i => new AssignItemRow
            {
                ItemId = i.Id,
                ItemName = i.Name ?? string.Empty,
                CategoryName = i.Category?.Name ?? string.Empty,
                Description = i.Description ?? string.Empty,
                Quantity = existingAssignments.FirstOrDefault(ic => ic.ItemId == i.Id)?.Quantity ?? 1,
                IsEquipped = existingAssignments.FirstOrDefault(ic => ic.ItemId == i.Id)?.IsEquipped ?? true,
                Selected = existingAssignments.Any(ic => ic.ItemId == i.Id)
            }).ToList();

            return new CharacterAssignItemsViewModel
            {
                CharacterId = character.Id,
                Character = character,
                Assignments = assignments,
                Page = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
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
