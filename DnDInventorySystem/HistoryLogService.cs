using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnDInventorySystem.Data;
using DnDInventorySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDInventorySystem
{
    public class HistoryLogService
    {
        private readonly ApplicationDbContext _context;

        public HistoryLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(int gameId, int userId, string action, string details, int? characterId = null, int? itemId = null, int? categoryId = null)
        {
            var log = new HistoryLog
            {
                GameId = gameId,
                UserId = userId,
                Action = action,
                Details = details,
                CharacterId = characterId,
                ItemId = itemId,
                CategoryId = categoryId,
                Timestamp = DateTime.UtcNow
            };

            _context.HistoryLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<HistoryLog>> GetRecentAsync(int gameId, int requestingUserId, bool isOwner, int take = 30)
        {
            var query = _context.HistoryLogs
                .Where(h => h.GameId == gameId)
                .Include(h => h.User)
                .Include(h => h.Character)
                .Include(h => h.Item)
                .Include(h => h.Category)
                .AsQueryable();

            if (!isOwner)
            {
                var playerCharacterIds = await _context.Characters
                    .Where(c => c.GameId == gameId &&
                                (c.OwnerUserId == requestingUserId || c.CreatedByUserId == requestingUserId))
                    .Select(c => c.Id)
                    .ToListAsync();

                var playerItemIds = await _context.Items
                    .Where(i => i.GameId == gameId && i.CreatedByUserId == requestingUserId)
                    .Select(i => i.Id)
                    .ToListAsync();

                var itemsOnPlayerCharacters = await _context.ItemCharacters
                    .Where(ic => ic.Character.GameId == gameId && playerCharacterIds.Contains(ic.CharacterId))
                    .Select(ic => ic.ItemId)
                    .Distinct()
                    .ToListAsync();

                var relevantItemIds = new HashSet<int>(playerItemIds.Concat(itemsOnPlayerCharacters));

                query = query.Where(h =>
                    h.UserId == requestingUserId ||
                    (h.CharacterId.HasValue && playerCharacterIds.Contains(h.CharacterId.Value)) ||
                    (h.ItemId.HasValue && relevantItemIds.Contains(h.ItemId.Value)));
            }

            return await query
                .OrderByDescending(h => h.Timestamp)
                .Take(take)
                .ToListAsync();
        }
    }
}
