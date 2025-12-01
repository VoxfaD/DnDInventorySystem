using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DnDInventorySystem.Data;
using DnDInventorySystem.Models;
using DnDInventorySystem.ViewModels;

namespace DnDInventorySystem.Controllers
{
    [Authorize]
    public class CharactersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CharactersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Characters
        public async Task<IActionResult> Index(int? gameId)
        {
            var charactersQuery = _context.Characters
                .Include(c => c.CreatedByUser)
                .Include(c => c.Game)
                .Include(c => c.Owner)
                .AsQueryable();

            if (gameId.HasValue)
            {
                charactersQuery = charactersQuery.Where(c => c.GameId == gameId.Value);
                ViewData["CurrentGameId"] = gameId.Value;
                ViewData["CurrentGameName"] = await _context.Games
                    .Where(g => g.Id == gameId.Value)
                    .Select(g => g.Name)
                    .FirstOrDefaultAsync();
            }

            return View(await charactersQuery.ToListAsync());
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

            var viewModel = await BuildCharacterInventoryViewModel(character);
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

            await PopulateCharacterCreateViewAsync(game);
            return View();
        }

        // POST: Characters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int gameId, [Bind("Name,Description,PhotoUrl,OwnerUserId")] Character character)
        {
            var game = await GetAuthorizedGameAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            var validOwners = await GetGameUsersAsync(game);
            if (!validOwners.Any(u => u.Id == character.OwnerUserId))
            {
                ModelState.AddModelError(nameof(character.OwnerUserId), "Select a user who is part of this game.");
            }

            character.GameId = game.Id;
            character.CreatedByUserId = GetCurrentUserId();

            if (ModelState.IsValid)
            {
                _context.Add(character);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Games", new { id = game.Id });
            }

            await PopulateCharacterCreateViewAsync(game, character.OwnerUserId, validOwners);
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

            await PopulateCharacterEditViewAsync(character, character.OwnerUserId);
            return View(character);
        }

        // POST: Characters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,PhotoUrl,OwnerUserId")] Character formCharacter)
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

            var validOwners = await GetGameUsersAsync(character.Game);
            if (!validOwners.Any(u => u.Id == formCharacter.OwnerUserId))
            {
                ModelState.AddModelError(nameof(formCharacter.OwnerUserId), "Select a user who is part of this game.");
            }

            if (ModelState.IsValid)
            {
                character.Name = formCharacter.Name;
                character.Description = formCharacter.Description;
                character.PhotoUrl = formCharacter.PhotoUrl;
                character.OwnerUserId = formCharacter.OwnerUserId;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { gameId = character.GameId });
            }

            await PopulateCharacterEditViewAsync(character, formCharacter.OwnerUserId, validOwners);
            character.Name = formCharacter.Name;
            character.Description = formCharacter.Description;
            character.PhotoUrl = formCharacter.PhotoUrl;
            character.OwnerUserId = formCharacter.OwnerUserId;
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

            return View(character);
        }

        // POST: Characters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var character = await _context.Characters.FindAsync(id);
            if (character != null)
            {
                _context.Characters.Remove(character);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
                     g.RolePersons.Any(rp => rp.UserId == userId)));
        }

        private async Task<List<User>> GetGameUsersAsync(Game game)
        {
            var participantIds = await _context.RolePersons
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

        private async Task PopulateCharacterCreateViewAsync(Game game, int? selectedOwnerId = null, IEnumerable<User> owners = null)
        {
            var ownerList = owners ?? await GetGameUsersAsync(game);
            ViewData["OwnerUserId"] = new SelectList(ownerList, "Id", "Name", selectedOwnerId);
            ViewData["CurrentGameId"] = game.Id;
            ViewData["CurrentGameName"] = game.Name;
        }

        private async Task PopulateCharacterEditViewAsync(Character character, int? selectedOwnerId = null, IEnumerable<User> owners = null)
        {
            var ownerList = owners ?? await GetGameUsersAsync(character.Game);
            ViewData["OwnerUserId"] = new SelectList(ownerList, "Id", "Name", selectedOwnerId ?? character.OwnerUserId);
        }

        // GET: Characters/AssignItems/5
        public async Task<IActionResult> AssignItems(int id)
        {
            var character = await GetAuthorizedCharacterAsync(id);
            if (character == null)
            {
                return NotFound();
            }

            var viewModel = await BuildCharacterAssignItemsViewModel(character);
            return View(viewModel);
        }

        // POST: Characters/AssignItems
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignItems(CharacterAssignItemsViewModel model)
        {
            var character = await GetAuthorizedCharacterAsync(model.Character.Id);
            if (character == null)
            {
                return NotFound();
            }

            if (model.Assignments == null || !model.Assignments.Any(a => a.Selected))
            {
                ModelState.AddModelError(string.Empty, "Select at least one item to assign.");
            }

            if (!ModelState.IsValid)
            {
                var vm = await BuildCharacterAssignItemsViewModel(character);
                return View(vm);
            }

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
                }
                else
                {
                    existing.Quantity = row.Quantity;
                    existing.IsEquipped = row.IsEquipped;
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
                Inventory = inventory
            };
        }

        private async Task<CharacterAssignItemsViewModel> BuildCharacterAssignItemsViewModel(Character character)
        {
            var items = await _context.Items
                .Where(i => i.GameId == character.GameId)
                .OrderBy(i => i.Name)
                .Include(i => i.Category)
                .ToListAsync();

            var assignments = items.Select(i => new AssignItemRow
            {
                ItemId = i.Id,
                ItemName = i.Name,
                CategoryName = i.Category?.Name,
                Description = i.Description,
                Quantity = 1,
                IsEquipped = true,
                Selected = false
            }).ToList();

            return new CharacterAssignItemsViewModel
            {
                Character = character,
                Assignments = assignments
            };
        }
    }
}
