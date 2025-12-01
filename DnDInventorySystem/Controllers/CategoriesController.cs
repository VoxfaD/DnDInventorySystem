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

namespace DnDInventorySystem.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index(int? gameId)
        {
            if (!gameId.HasValue)
            {
                return NotFound();
            }

            var game = await GetAuthorizedGameAsync(gameId.Value);
            if (game == null)
            {
                return NotFound();
            }

            var categories = await _context.Categories
                .Include(c => c.CreatedByUser)
                .Include(c => c.Game)
                .Where(c => c.GameId == game.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewData["CurrentGameId"] = game.Id;
            ViewData["CurrentGameName"] = game.Name;
            return View(categories);
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

            PopulateCategoryCreateView(game);
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

            category.GameId = game.Id;
            category.CreatedByUserId = GetCurrentUserId();

            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Games", new { id = game.Id });
            }

            PopulateCategoryCreateView(game);
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
            ViewData["CreatedByUserId"] = new SelectList(_context.Users, "Id", "Id", category.CreatedByUserId);
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "Name", category.GameId);
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,GameId,CreatedByUserId")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CreatedByUserId"] = new SelectList(_context.Users, "Id", "Id", category.CreatedByUserId);
            ViewData["GameId"] = new SelectList(_context.Games, "Id", "Name", category.GameId);
            return View(category);
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

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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

        private Task<Game> GetAuthorizedGameAsync(int gameId)
        {
            var userId = GetCurrentUserId();
            return _context.Games
                .FirstOrDefaultAsync(g => g.Id == gameId &&
                    (g.CreatedByUserId == userId ||
                     g.RolePersons.Any(rp => rp.UserId == userId)));
        }

        private void PopulateCategoryCreateView(Game game)
        {
            ViewData["CurrentGameId"] = game.Id;
            ViewData["CurrentGameName"] = game.Name;
        }
    }
}
