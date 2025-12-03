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

        public async Task<List<HistoryLog>> GetRecentAsync(int gameId, int take = 30)
        {
            return await _context.HistoryLogs
                .Where(h => h.GameId == gameId)
                .Include(h => h.User)
                .Include(h => h.Character)
                .Include(h => h.Item)
                .Include(h => h.Category)
                .OrderByDescending(h => h.Timestamp)
                .Take(take)
                .ToListAsync();
        }
    }
}
