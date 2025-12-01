using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DnDInventorySystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Categories_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Characters_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Characters_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePersons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePersons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePersons_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePersons_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePersons_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Items_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoryLogs_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoryLogs_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoryLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemCharacters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    CharacterId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IsEquipped = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCharacters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCharacters_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemCharacters_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Name", "PasswordHash" },
                values: new object[,]
                {
                    { 1, "alice@example.com", "Alice", "Password123!" },
                    { 2, "bob@example.com", "Bob", "Swordfish1!" }
                });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Alice's campaign", "Stormreach" },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Bob's campaign", "Duskhaven" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedByUserId", "GameId", "Name" },
                values: new object[,]
                {
                    { 1, 1, 1, "Weapons" },
                    { 2, 1, 1, "Armor" },
                    { 3, 1, 1, "Potions" },
                    { 4, 1, 1, "Scrolls" },
                    { 5, 1, 1, "Tools" },
                    { 6, 1, 1, "Food" },
                    { 7, 1, 1, "Trinkets" },
                    { 8, 1, 1, "Quest Items" },
                    { 9, 1, 1, "Materials" },
                    { 10, 1, 1, "Misc" },
                    { 101, 2, 2, "Weapons" },
                    { 102, 2, 2, "Armor" },
                    { 103, 2, 2, "Potions" },
                    { 104, 2, 2, "Scrolls" },
                    { 105, 2, 2, "Tools" },
                    { 106, 2, 2, "Food" },
                    { 107, 2, 2, "Trinkets" },
                    { 108, 2, 2, "Quest Items" },
                    { 109, 2, 2, "Materials" },
                    { 110, 2, 2, "Misc" }
                });

            migrationBuilder.InsertData(
                table: "Characters",
                columns: new[] { "Id", "CreatedByUserId", "Description", "GameId", "Name", "OwnerUserId", "PhotoUrl" },
                values: new object[,]
                {
                    { 1, 1, "Ranger", 1, "Aria", 1, "" },
                    { 2, 1, "Cleric", 1, "Bram", 1, "" },
                    { 3, 1, "Wizard", 1, "Celeste", 1, "" },
                    { 101, 2, "Fighter", 2, "Dante", 2, "" },
                    { 102, 2, "Druid", 2, "Elara", 2, "" },
                    { 103, 2, "Rogue", 2, "Felix", 2, "" }
                });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "CategoryId", "CreatedByUserId", "Description", "GameId", "Name", "PhotoUrl" },
                values: new object[,]
                {
                    { 1, 1, 1, "Longsword description", 1, "Longsword", "" },
                    { 2, 2, 1, "Shield description", 1, "Shield", "" },
                    { 3, 3, 1, "Shortbow description", 1, "Shortbow", "" },
                    { 4, 4, 1, "Dagger description", 1, "Dagger", "" },
                    { 5, 5, 1, "Quarterstaff description", 1, "Quarterstaff", "" },
                    { 6, 6, 1, "Healing Potion description", 1, "Healing Potion", "" },
                    { 7, 7, 1, "Mana Potion description", 1, "Mana Potion", "" },
                    { 8, 8, 1, "Rope description", 1, "Rope", "" },
                    { 9, 9, 1, "Lockpick Set description", 1, "Lockpick Set", "" },
                    { 10, 10, 1, "Torch description", 1, "Torch", "" },
                    { 11, 1, 1, "Map description", 1, "Map", "" },
                    { 12, 2, 1, "Silver Ring description", 1, "Silver Ring", "" },
                    { 13, 3, 1, "Chain Shirt description", 1, "Chain Shirt", "" },
                    { 14, 4, 1, "Bag of Holding description", 1, "Bag of Holding", "" },
                    { 15, 5, 1, "Scroll of Fireball description", 1, "Scroll of Fireball", "" },
                    { 101, 101, 2, "Battleaxe description", 2, "Battleaxe", "" },
                    { 102, 102, 2, "Buckler description", 2, "Buckler", "" },
                    { 103, 103, 2, "Crossbow description", 2, "Crossbow", "" },
                    { 104, 104, 2, "Stiletto description", 2, "Stiletto", "" },
                    { 105, 105, 2, "Wand description", 2, "Wand", "" },
                    { 106, 106, 2, "Elixir of Health description", 2, "Elixir of Health", "" },
                    { 107, 107, 2, "Stamina Draught description", 2, "Stamina Draught", "" },
                    { 108, 108, 2, "Grappling Hook description", 2, "Grappling Hook", "" },
                    { 109, 109, 2, "Thieves' Tools description", 2, "Thieves' Tools", "" },
                    { 110, 110, 2, "Lantern description", 2, "Lantern", "" },
                    { 111, 101, 2, "Compass description", 2, "Compass", "" },
                    { 112, 102, 2, "Gold Amulet description", 2, "Gold Amulet", "" },
                    { 113, 103, 2, "Scale Mail description", 2, "Scale Mail", "" },
                    { 114, 104, 2, "Handy Haversack description", 2, "Handy Haversack", "" },
                    { 115, 105, 2, "Scroll of Lightning description", 2, "Scroll of Lightning", "" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CreatedByUserId",
                table: "Categories",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_GameId",
                table: "Categories",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CreatedByUserId",
                table: "Characters",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_GameId",
                table: "Characters",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_OwnerUserId",
                table: "Characters",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CreatedByUserId",
                table: "Games",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryLogs_CharacterId",
                table: "HistoryLogs",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryLogs_ItemId",
                table: "HistoryLogs",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryLogs_UserId",
                table: "HistoryLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCharacters_CharacterId_ItemId",
                table: "ItemCharacters",
                columns: new[] { "CharacterId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCharacters_ItemId",
                table: "ItemCharacters",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                table: "Items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CreatedByUserId",
                table: "Items",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_GameId",
                table: "Items",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePersons_GameId",
                table: "RolePersons",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePersons_RoleId",
                table: "RolePersons",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePersons_UserId",
                table: "RolePersons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoryLogs");

            migrationBuilder.DropTable(
                name: "ItemCharacters");

            migrationBuilder.DropTable(
                name: "RolePersons");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
