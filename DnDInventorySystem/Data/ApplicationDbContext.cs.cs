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
        }
    }
}
