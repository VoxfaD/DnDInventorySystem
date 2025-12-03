using DnDInventorySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDInventorySystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePerson> RolePersons { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemCharacter> ItemCharacters { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<HistoryLog> HistoryLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

                            // --- SEED DATA ---
            var user1 = new User { Id = 1, Name = "Alice", Email = "alice@example.com", PasswordHash = "Password123!" };
            var user2 = new User { Id = 2, Name = "Bob",   Email = "bob@example.com",   PasswordHash = "Swordfish1!" };

            var game1 = new Game { Id = 1, Name = "Stormreach", Description = "Alice's campaign", CreatedByUserId = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            var game2 = new Game { Id = 2, Name = "Duskhaven",  Description = "Bob's campaign",   CreatedByUserId = 2, CreatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc) };

            string[] categoryNames = { "Weapons","Armor","Potions","Scrolls","Tools","Food","Trinkets","Quest Items","Materials","Misc" };
            var categories = categoryNames.Select((n, i) => new Category { Id = i + 1, Name = n, GameId = 1, CreatedByUserId = 1 })
                .Concat(categoryNames.Select((n, i) => new Category { Id = i + 101, Name = n, GameId = 2, CreatedByUserId = 2 }))
                .ToArray();

            // items for game 1
            var items1 = new[]
            {
                "Longsword","Shield","Shortbow","Dagger","Quarterstaff","Healing Potion","Mana Potion","Rope","Lockpick Set","Torch",
                "Map","Silver Ring","Chain Shirt","Bag of Holding","Scroll of Fireball"
            }.Select((n,i) => new Item
            {
                Id = i + 1,
                Name = n,
                Description = $"{n} description",
                GameId = 1,
                CreatedByUserId = 1,
                CategoryId = categories[i % 10].Id,          // ← use first 10 categories
                PhotoUrl = ""
            }).ToArray();

            // items for game 2
            var items2 = new[]
            {
                "Battleaxe","Buckler","Crossbow","Stiletto","Wand","Elixir of Health","Stamina Draught","Grappling Hook","Thieves' Tools","Lantern",
                "Compass","Gold Amulet","Scale Mail","Handy Haversack","Scroll of Lightning"
            }.Select((n,i) => new Item
            {
                Id = i + 101,
                Name = n,
                Description = $"{n} description",
                GameId = 2,
                CreatedByUserId = 2,
                CategoryId = categories[10 + (i % 10)].Id,   // ← use the 10 categories for game 2
                PhotoUrl = ""
            }).ToArray();


            var chars1 = new[]
            {
                new Character { Id = 1, Name = "Aria", Description = "Ranger", GameId = 1, CreatedByUserId = 1, OwnerUserId = 1 },
                new Character { Id = 2, Name = "Bram", Description = "Cleric", GameId = 1, CreatedByUserId = 1, OwnerUserId = 1 },
                new Character { Id = 3, Name = "Celeste", Description = "Wizard", GameId = 1, CreatedByUserId = 1, OwnerUserId = 1 }
            };
            var chars2 = new[]
            {
                new Character { Id = 101, Name = "Dante", Description = "Fighter", GameId = 2, CreatedByUserId = 2, OwnerUserId = 2 },
                new Character { Id = 102, Name = "Elara", Description = "Druid",  GameId = 2, CreatedByUserId = 2, OwnerUserId = 2 },
                new Character { Id = 103, Name = "Felix", Description = "Rogue",  GameId = 2, CreatedByUserId = 2, OwnerUserId = 2 }
            };

            modelBuilder.Entity<User>().HasData(user1, user2);
            modelBuilder.Entity<Game>().HasData(game1, game2);
            modelBuilder.Entity<Category>().HasData(categories);
            modelBuilder.Entity<Item>().HasData(items1.Concat(items2).ToArray());
            modelBuilder.Entity<Character>().HasData(chars1.Concat(chars2).ToArray());
            // --- END SEED DATA ---

            // Unique email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // GAME
            modelBuilder.Entity<Game>()
                .HasOne(g => g.CreatedByUser)
                .WithMany(u => u.CreatedGames)
                .HasForeignKey(g => g.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict); // keep - avoids huge cascades

            // CATEGORY
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Game)
                .WithMany(g => g.Categories)
                .HasForeignKey(c => c.GameId)
                .OnDelete(DeleteBehavior.Restrict); // was Cascade; make Restrict to simplify graph

            modelBuilder.Entity<Category>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // CHARACTER
            modelBuilder.Entity<Character>()
                .HasOne(c => c.Game)
                .WithMany(g => g.Characters)
                .HasForeignKey(c => c.GameId)
                .OnDelete(DeleteBehavior.Restrict); // was Cascade; make Restrict

            modelBuilder.Entity<Character>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Character>()
                .HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ITEM
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Game)
                .WithMany(g => g.Items)
                .HasForeignKey(i => i.GameId)
                .OnDelete(DeleteBehavior.Restrict); // <<< CHANGE: was Cascade → Restrict

            modelBuilder.Entity<Item>()
                .HasOne(i => i.CreatedByUser)
                .WithMany()
                .HasForeignKey(i => i.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Item>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // ITEM-CHARACTER (make both sides Restrict to avoid future cascades)
            modelBuilder.Entity<ItemCharacter>()
                .HasOne(ic => ic.Item)
                .WithMany(i => i.ItemCharacters)
                .HasForeignKey(ic => ic.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemCharacter>()
                .HasOne(ic => ic.Character)
                .WithMany(c => c.ItemCharacters)
                .HasForeignKey(ic => ic.CharacterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemCharacter>()
                .HasIndex(ic => new { ic.CharacterId, ic.ItemId })
                .IsUnique();

            // ROLE-PERSON
            modelBuilder.Entity<RolePerson>()
                .HasOne(rp => rp.User)
                .WithMany()
                .HasForeignKey(rp => rp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePerson>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePersons)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePerson>()
                .HasOne(rp => rp.Game)
                .WithMany(g => g.RolePersons)
                .HasForeignKey(rp => rp.GameId)
                .OnDelete(DeleteBehavior.Restrict);

            // HISTORY LOG
            modelBuilder.Entity<HistoryLog>()
                .HasOne(h => h.Game)
                .WithMany(g => g.HistoryLogs)
                .HasForeignKey(h => h.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HistoryLog>()
                .HasOne(h => h.Item)
                .WithMany(i => i.HistoryLogs)
                .HasForeignKey(h => h.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoryLog>()
                .HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoryLog>()
                .HasOne(h => h.Character)
                .WithMany()
                .HasForeignKey(h => h.CharacterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoryLog>()
                .HasOne(h => h.Category)
                .WithMany()
                .HasForeignKey(h => h.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
