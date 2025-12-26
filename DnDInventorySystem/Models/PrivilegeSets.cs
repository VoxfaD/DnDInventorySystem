namespace DnDInventorySystem.Models
{
    public static class PrivilegeSets
    {
        public const GamePrivilege Owner = GamePrivilege.All;

        public const GamePrivilege Player =
            GamePrivilege.CreateCharacters |
            GamePrivilege.EditCharacters |
            GamePrivilege.CreateItems |
            GamePrivilege.EditItems |
            GamePrivilege.DeleteItems |
            GamePrivilege.ViewCharacters |
            GamePrivilege.ViewItems |
            GamePrivilege.ViewCategories |
            GamePrivilege.AddItemsToCharacters |
            GamePrivilege.RemoveItemsFromCharacters |
            GamePrivilege.EditCharacterInventory |
            GamePrivilege.ViewHistoryLogs;
    }
}
