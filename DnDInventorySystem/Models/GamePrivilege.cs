using System;

namespace DnDInventorySystem.Models
{
    [Flags]
    public enum GamePrivilege
    {
        None = 0,
        EditGame = 1 << 0,
        CreateJoinCode = 1 << 1,
        ActivateJoinCode = 1 << 2,
        CreateCharacters = 1 << 3,
        EditCharacters = 1 << 4,
        DeleteCharacters = 1 << 5,
        CreateItems = 1 << 6,
        EditItems = 1 << 7,
        DeleteItems = 1 << 8,
        CreateCategories = 1 << 9,
        EditCategories = 1 << 10,
        DeleteCategories = 1 << 11,
        ViewCharacters = 1 << 12,
        ViewItems = 1 << 13,
        ViewCategories = 1 << 14,
        AddItemsToCharacters = 1 << 15,
        RemoveItemsFromCharacters = 1 << 16,
        EditCharacterInventory = 1 << 17,
        ViewHistoryLogs = 1 << 18,
        RemovePlayers = 1 << 19,
        All = ~0
    }
}
